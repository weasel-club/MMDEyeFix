#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Goorm.MMDEyeFix
{
    [CustomEditor(typeof(MMDEyeFix))]
    public class MMDEyeFixEditor : Editor
    {
        private class BlendShapeList
        {
            private readonly List<string> _list;
            private Vector2 _scrollPosition;
            private string _search = "";
            private readonly Dictionary<string, int> _memo = new();

            public BlendShapeList(List<string> list)
            {
                _list = list;
            }

            private int GetBlendShapeNameScore(string name, string[][] keywords)
            {
                if (_memo.TryGetValue(name, out var score)) return score;

                score = keywords.Sum(keyword => keyword.Count(word =>
                {
                    var lower = word.ToLower();
                    var upper = char.ToUpper(lower[0]) + lower[1..];
                    return name.Contains(upper) ||
                           Regex.IsMatch(name, $"^.+_{lower}(_.*)?$") ||
                           Regex.IsMatch(name, $"^(.*_)?{lower}_.+$");
                }));
                if (score > 0) score += keywords.Length;

                score += keywords.Count(keyword => keyword.Any(word =>
                {
                    var lower = word.ToLower();
                    var upper = char.ToUpper(lower[0]) + lower[1..];
                    return name.Contains(lower) || name.Contains(upper);
                }));

                _memo[name] = score;
                return score;
            }

            private static GUIStyle BoxStyle => new("HelpBox")
            {
                padding = new RectOffset(16, 16, 8, 8),
                margin = new RectOffset(0, 0, 0, 0)
            };

            public void DrawEditor(
                string label, SkinnedMeshRenderer renderer, ref float weight, params string[][] prioritizeKeywords
            )
            {
                DrawEditor(label, renderer, ref weight, false, prioritizeKeywords);
            }

            public void DrawEditor(
                string label, SkinnedMeshRenderer renderer, ref float weight, bool showAll, params string[][] prioritizeKeywords
            )
            {
                GUILayout.BeginVertical(BoxStyle);
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                DrawSelector(renderer, showAll, prioritizeKeywords);
                if (weight >= 0f)
                {
                    weight = DrawWeightSlider(weight);
                }
                GUILayout.EndVertical();
            }

            private string _lastSearch = "";
            private bool _shouldCalculate = false;
            private List<string> _cachedFilteredNames = new();
            private GUIStyle _buttonStyle;
            private List<string> _cachedNames = new();
            private int _lastMeshId = -1;

            private void DrawSelector(SkinnedMeshRenderer renderer, bool showAll, params string[][] prioritizeKeywords)
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle(GUI.skin.button)
                    {
                        wordWrap = true,
                        richText = true
                    };
                }

                var newSearch = EditorGUILayout.TextField("Search (Separate by space)", _search);

                // 메시가 변경되었는지 확인
                var currentMeshId = renderer.sharedMesh.GetInstanceID();
                var meshChanged = currentMeshId != _lastMeshId;
                if (meshChanged)
                {
                    _lastMeshId = currentMeshId;
                    _cachedNames = renderer.sharedMesh.GetBlendShapeNames();
                    _cachedFilteredNames.Clear(); // 메시가 변경되면 필터링된 결과도 초기화
                    _shouldCalculate = true;
                }

                // 검색어가 변경되었을 때
                if (newSearch != _lastSearch)
                {
                    _search = newSearch;
                    _lastSearch = newSearch;
                    _shouldCalculate = true;
                }

                // 검색어가 변경되었거나 메시가 변경되었거나 캐시가 비어있을 때만 필터링 수행
                if (_shouldCalculate)
                {
                    Debug.Log("Calculate BlendShapeNames");
                    _shouldCalculate = false;

                    var searchWords = _search.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    // 필터링 및 정렬을 한 번에 처리
                    _cachedFilteredNames = _cachedNames
                        .Where(name =>
                        {
                            if (!showAll && renderer.GetBlendShapeWeight(name) <= 0f)
                                return false;

                            if (searchWords.Length == 0)
                                return true;

                            var lowerName = name.ToLower();
                            return searchWords.All(word => lowerName.Contains(word));
                        })
                        .ToList();

                    // 정렬 로직 최적화
                    if (prioritizeKeywords.Length > 0)
                    {
                        var scores = new Dictionary<string, int>();
                        foreach (var name in _cachedFilteredNames)
                        {
                            scores[name] = GetBlendShapeNameScore(name, prioritizeKeywords);
                        }

                        _cachedFilteredNames.Sort((a, b) =>
                        {
                            var containsA = _list.Contains(a);
                            var containsB = _list.Contains(b);
                            if (containsA != containsB) return containsB.CompareTo(containsA);
                            return scores[b].CompareTo(scores[a]);
                        });
                    }
                }

                var columns = Mathf.Min(_cachedFilteredNames.Count, 4);
                var width = EditorGUIUtility.currentViewWidth - 80;
                var columnWidth = width / columns;

                _scrollPosition = EditorGUILayout.BeginScrollView(
                    _scrollPosition, false, false, GUIStyle.none,
                    GUI.skin.verticalScrollbar, GUI.skin.box, GUILayout.Height(150));

                for (var i = 0; i < _cachedFilteredNames.Count; i += columns)
                {
                    EditorGUILayout.BeginHorizontal();

                    for (var j = 0; j < columns && i + j < _cachedFilteredNames.Count; j++)
                    {
                        var name = _cachedFilteredNames[i + j];
                        var score = GetBlendShapeNameScore(name, prioritizeKeywords);

                        var originalColor = GUI.backgroundColor;
                        if (_list.Contains(name))
                            GUI.backgroundColor = Color.green;
                        else if (score >= 3 * prioritizeKeywords.Length)
                            GUI.backgroundColor = Color.yellow;

                        var value = renderer.GetBlendShapeWeight(renderer.sharedMesh.GetBlendShapeIndex(name));
                        var tooltip = $"{name} ({value})";

                        if (GUILayout.Button(new GUIContent(name, tooltip), _buttonStyle, GUILayout.Width(columnWidth)))
                        {
                            if (_list.Contains(name)) _list.Remove(name);
                            else _list.Add(name);
                        }

                        GUI.backgroundColor = originalColor;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            private float DrawWeightSlider(float value)
            {
                return EditorGUILayout.Slider("Weight", value, 0f, 1f);
            }
        }


        private BlendShapeList _leftEyeBlendShapes;
        private BlendShapeList _rightEyeBlendShapes;
        private BlendShapeList _mouthBlendShapes;

        private BlendShapeList _customLeftEyeBlendShapes;
        private BlendShapeList _customRightEyeBlendShapes;
        private BlendShapeList _customMouthBlendShapes;

        private void OnEnable()
        {
            var optimizer = (MMDEyeFix)target;

            _leftEyeBlendShapes = new BlendShapeList(optimizer._leftEyeBlendShapes);
            _rightEyeBlendShapes = new BlendShapeList(optimizer._rightEyeBlendShapes);
            _mouthBlendShapes = new BlendShapeList(optimizer._mouthBlendShapes);

            _customLeftEyeBlendShapes = new BlendShapeList(optimizer._customLeftEyeBlendShapes);
            _customRightEyeBlendShapes = new BlendShapeList(optimizer._customRightEyeBlendShapes);
            _customMouthBlendShapes = new BlendShapeList(optimizer._customMouthBlendShapes);
        }

        public override void OnInspectorGUI()
        {
            var optimizer = (MMDEyeFix)target;

            if (optimizer.IsApplied)
            {
                EditorGUILayout.HelpBox("MMDEyeFix is applied. Please revert before editing.", MessageType.Warning);

                if (GUILayout.Button("Revert")) optimizer.Revert();
            }
            else
            {
                var referenceObject = optimizer._faceRenderer.Get(optimizer);
                var faceRenderer = EditorGUILayout.ObjectField(
                    new GUIContent("Face Renderer"),
                    referenceObject?.GetComponent<SkinnedMeshRenderer>(),
                    typeof(SkinnedMeshRenderer),
                    true
                ) as SkinnedMeshRenderer;
                optimizer._faceRenderer.Set(faceRenderer ? faceRenderer.gameObject : null);
                EditorGUILayout.Space();

                if (faceRenderer)
                {
                    _leftEyeBlendShapes.DrawEditor(
                        "Left Eye BlendShapes",
                        faceRenderer,
                        ref optimizer._leftEyeWeight,
                        new[] { "eye" }, new[] { "left", "l" }
                    );
                    EditorGUILayout.Space();

                    _rightEyeBlendShapes.DrawEditor(
                        "Right Eye BlendShapes",
                        faceRenderer,
                        ref optimizer._rightEyeWeight,
                        new[] { "eye" }, new[] { "right", "r" }
                    );
                    EditorGUILayout.Space();

                    _mouthBlendShapes.DrawEditor(
                        "Mouth BlendShapes",
                        faceRenderer,
                        ref optimizer._mouthWeight,
                        new[] { "mouth" }
                    );
                    if (GUILayout.Button("Apply (For test, it should be reverted)")) optimizer.Apply();

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    optimizer._applyToCustomBlendShapes = EditorGUILayout.Toggle(
                        optimizer._applyToCustomBlendShapes,
                        GUILayout.ExpandWidth(false),
                        GUILayout.Width(20)
                    );
                    EditorGUILayout.LabelField(
                        "Apply to custom non-MMD blend shapes",
                        EditorStyles.boldLabel, GUILayout.ExpandWidth(true)
                    );
                    EditorGUILayout.EndHorizontal();

                    if (optimizer._applyToCustomBlendShapes)
                    {
                        var disable = -1f;
                        _customLeftEyeBlendShapes.DrawEditor(
                            "Left Eye BlendShapes",
                            faceRenderer,
                            ref disable,
                            true,
                            new[] { "eye" }, new[] { "left", "l" }
                        );

                        _customRightEyeBlendShapes.DrawEditor(
                            "Right Eye BlendShapes",
                            faceRenderer,
                            ref disable,
                            true,
                            new[] { "eye" }, new[] { "right", "r" }
                        );

                        _customMouthBlendShapes.DrawEditor(
                            "Mouth BlendShapes",
                            faceRenderer,
                            ref disable,
                            true,
                            new[] { "mouth" }
                        );
                    }
                }

            }



            EditorUtility.SetDirty(target);
        }

        private void OnDisable()
        {
            var optimizer = (MMDEyeFix)target;
            if (optimizer != null) optimizer.Revert();
        }
    }
}

#endif