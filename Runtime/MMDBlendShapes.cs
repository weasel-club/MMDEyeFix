#if UNITY_EDITOR

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Goorm.MMDEyeFix
{
    [Flags]
    public enum MmdBlendShapeCategory
    {
        LeftEye = 1,
        RightEye = 2,
        Mouth = 4
    }

    public class MmdBlendShape
    {
        public readonly string[] Names;
        public readonly MmdBlendShapeCategory Category;

        public MmdBlendShape(MmdBlendShapeCategory category, params string[] names)
        {
            Names = names;
            Category = category;
        }
    }

    public static class MmdBlendShapes
    {
        public static MmdBlendShape Get(string name)
        {
            return All.Find(blendShape => blendShape.Names.Contains(name));
        }

        private static readonly ImmutableList<MmdBlendShape> All = ImmutableList.Create(
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "あ", "a", "Ah"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "い", "i", "Ch"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "う", "u", "U"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "え", "e", "E"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "お", "o", "Oh"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "にやり", "Niyari", "Grin"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "∧", "Mouse_2", "∧"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "ワ", "Wa", "Wa"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "ω", "Omega", "ω"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "▲", "Mouse_1", "▲"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "口角上げ", "MouseUP", "Mouth Horn Raise"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "口角下げ", "MouseDW", "Mouth Horn Lower"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "口横広げ", "MouseWD", "Mouth Side Widen"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "にやり２", "Niyari2"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "ん", "n"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "あ２", "a 2"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "□", "□"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "ω□", "ω□"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "にっこり", "Smile"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "ぺろっ", "Pero"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "てへぺろ", "Bero-tehe"),
            new MmdBlendShape(MmdBlendShapeCategory.Mouth, "てへぺろ２", "Bero-tehe2"),
            new MmdBlendShape(MmdBlendShapeCategory.LeftEye | MmdBlendShapeCategory.RightEye, "まばたき", "Blink", "Blink"),
            new MmdBlendShape(MmdBlendShapeCategory.LeftEye | MmdBlendShapeCategory.RightEye, "笑い", "Smile", "Blink Happy"),
            new MmdBlendShape(MmdBlendShapeCategory.LeftEye | MmdBlendShapeCategory.RightEye, "はぅ", "> <", "Close><"),
            new MmdBlendShape(MmdBlendShapeCategory.RightEye, "ｳｨﾝｸ２右", "Wink-c", "Wink 2 Right"),
            new MmdBlendShape(MmdBlendShapeCategory.LeftEye, "ウィンク２", "Wink-b", "Wink 2"),
            new MmdBlendShape(MmdBlendShapeCategory.LeftEye, "ウィンク", "Wink", "Wink"),
            new MmdBlendShape(MmdBlendShapeCategory.RightEye, "ウィンク右", "Wink-a", "Wink Right"),
            new MmdBlendShape(MmdBlendShapeCategory.LeftEye | MmdBlendShapeCategory.RightEye, "なごみ", "Howawa", "Calm"),
            new MmdBlendShape(MmdBlendShapeCategory.LeftEye | MmdBlendShapeCategory.RightEye, "はちゅ目", "O O"),
            new MmdBlendShape(MmdBlendShapeCategory.LeftEye | MmdBlendShapeCategory.RightEye, "喜び", "Joy"),
            new MmdBlendShape(MmdBlendShapeCategory.LeftEye | MmdBlendShapeCategory.RightEye, "なごみω", "Howawa ω")
        );
    }
}

#endif