using System;

namespace HoldemController.ConsoleDisplay
{
    internal class DrawArea
    {
        public DrawArea(int x, int y, int width, int height = 1, ConsoleColor? bgColor = null, ConsoleColor? fgColor = null)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
            _background = bgColor ?? ConsoleColor.Black;
            _foreground = fgColor ?? ConsoleColor.White;
        }

        private readonly int _x;
        private readonly int _y;
        private readonly int _width;
        private readonly int _height;
        private readonly ConsoleColor _background;
        private readonly ConsoleColor _foreground;

        public void Draw(string text, ConsoleColor? fgColor = null, ConsoleColor? bgColor = null)
        {
            DrawLines(new[] {text}, fgColor, bgColor);
        }

        public void DrawLines(string[] lines, ConsoleColor? fgColor = null, ConsoleColor? bgColor = null)
        {
            Console.ForegroundColor = fgColor ?? _foreground;
            Console.BackgroundColor = bgColor ??_background;
            for (var i = 0; i < Math.Max(lines.Length, _height); i++)
            {
                if (i >= _height) // too many lines
                {
                    break;
                }
                string text;
                if (i >= lines.Length) // not enough lines
                {
                    text = new string(' ', _width);
                }
                else
                {
                    text = lines[i];
                    text = (text?.Substring(0, Math.Min(text.Length, _width)) ?? string.Empty).PadRight(_width); // trim and pad    
                }
                Console.SetCursorPosition(_x, _y+i);
                Console.Write(text);
            }
            Console.ResetColor();
            Console.SetCursorPosition(0, 0);
        }
    }
}