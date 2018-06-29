using Microsoft.Xna.Framework.Input;

namespace Hourglass.Input
{
    public struct TextInputKey
    {
        public Keys Key { get; private set; }
        public char Upper { get; private set; }
        public char Lower { get; private set; }
        public char Shift { get; private set; }
        public bool IsSymbol { get; private set; }

        public static TextInputKey CreateLetter(Keys letterKey, char lower, char upper)
        {
            return new TextInputKey(letterKey, lower, upper, upper, false);
        }

        public static TextInputKey CreateSymbol(Keys symbolKey, char lower, char shift)
        {
            return new TextInputKey(symbolKey, lower, lower, shift, true);
        }

        private TextInputKey(Keys key, char lower, char upper, char shift, bool symbol)
        {
            Key = key;
            Lower = lower;
            Upper = upper;
            Shift = shift;
            IsSymbol = symbol;
        }

        //TODO: When c# 8 drops, replace with a return switch thingy they demoed during BUILD '18.
        public char GetChar(CapsType capsType)
        {
            switch (capsType)
            {
                case CapsType.CapsLockShift:
                    if (IsSymbol)
                    {
                        return Shift;
                    }
                    else
                    {
                        return Lower;
                    }
                case CapsType.CapsLock:
                    return Upper;
                case CapsType.Shift:
                    return Shift;
                default:
                    return Lower;
            }
        }
    }
}
