﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OpenVIII
{

    public class IGMData
    {

        #region Fields

        public bool[] BLANKS;
        /// <summary>
        /// location of where pointer finger will point.
        /// </summary>
        public Point[] CURSOR;

        public IGMDataItem[,] ITEM;
        /// <summary>
        /// Size of the entire area
        /// </summary>
        public Rectangle[] SIZE;

        protected Characters Character;
        protected bool skipdata = false;
        protected bool skipsnd = false;
        protected Characters VisableCharacter;
        private int _cursor_select;

        #endregion Fields

        #region Constructors

        public IGMData(int count = 0, int depth = 0, IGMDataItem container = null, int? cols = null, int? rows = null)
        => Init(count, depth, container, cols, rows);

        #endregion Constructors

        #region Properties
        public void SetModeChangeEvent(ref EventHandler<Enum> eventHandler) => eventHandler += ModeChangeEvent;

        protected virtual void ModeChangeEvent(object sender, Enum e)
        {
        }

        public int cols { get; private set; }
        public IGMDataItem CONTAINER { get; protected set; }
        /// <summary>
        /// Total number of items
        /// </summary>
        public byte Count { get; private set; }

        public int CURSOR_SELECT
        {
            get => GetCursor_select(); set
            {
                if ((Cursor_Status & Cursor_Status.Enabled) != 0 && value >= 0 && value < CURSOR.Length && CURSOR[value] != Point.Zero)
                    SetCursor_select(value);
            }
        }

        public Cursor_Status Cursor_Status { get; set; } = Cursor_Status.Disabled;
        /// <summary>
        /// How many Peices per Item. Example 1 box could have 9 things to draw in it.
        /// </summary>
        public byte Depth { get; private set; }

        public Dictionary<int, FF8String> Descriptions { get; protected set; }
        public bool Enabled { get; private set; } = true;

        public int rows { get; private set; }
        public Table_Options Table_Options { get; set; } = Table_Options.Default;
        public static Point MouseLocation => Menu.MouseLocation;

        public static Vector2 TextScale => Menu.TextScale;

        /// <summary>
        /// Container's Height
        /// </summary>
        public int Height => CONTAINER != null ? CONTAINER.Pos.Height : 0;

        /// <summary>
        /// Container's Width
        /// </summary>
        public int Width => CONTAINER != null ? CONTAINER.Pos.Width : 0;

        /// <summary>
        /// Container's X Position
        /// </summary>
        public int X => CONTAINER != null ? CONTAINER.Pos.X : 0;

        /// <summary>
        /// Container's Y Position
        /// </summary>
        public int Y => CONTAINER != null ? CONTAINER.Pos.Y : 0;

        #endregion Properties

        #region Indexers

        public IGMDataItem this[int pos, int i] { get => ITEM[pos, i]; set => ITEM[pos, i] = value; }

        #endregion Indexers

        #region Methods

        public static void DrawPointer(Point cursor, Vector2? offset = null, bool blink = false) => Menu.DrawPointer(cursor, offset, blink);

        /// <summary>
        /// Convert to rectangle based on container.
        /// </summary>
        /// <param name="v">Input data</param>
        public static implicit operator Rectangle(IGMData v) => v.CONTAINER ?? Rectangle.Empty;

        //public object PrevSetting { get; protected set; } = null;
        //public object Setting { get; protected set; } = null;
        public virtual int CURSOR_NEXT()
        {
            if ((Cursor_Status & Cursor_Status.Enabled) != 0)
            {
                int value = GetCursor_select();
                int loop = 0;
                while (true)
                {
                    if (++value >= CURSOR.Length)
                    {
                        value = 0;
                        if (loop++ > 1) break;
                    }
                    if ((CURSOR[value] != Point.Zero && !BLANKS[value])) break;
                }
                SetCursor_select(value);
            }
            return GetCursor_select();
        }

        public virtual int CURSOR_PREV()
        {
            if ((Cursor_Status & Cursor_Status.Enabled) != 0)
            {
                int value = GetCursor_select();
                int loop = 0;
                while (true)
                {
                    if (--value < 0)
                    {
                        value = CURSOR.Length - 1;
                        if (loop++ > 1) break;
                    }
                    if ((CURSOR[value] != Point.Zero && !BLANKS[value])) break;
                }
                SetCursor_select(value);
            }
            return GetCursor_select();
        }

        /// <summary>
        /// Draw all items
        /// </summary>
        public virtual void Draw()
        {
            if (Enabled)
            {
                if (CONTAINER != null)
                    CONTAINER.Draw();
                if (!skipdata && ITEM != null)
                    foreach (IGMDataItem i in ITEM)
                    {
                        if (i != null)
                            i.Draw();
                    }
                if ((Cursor_Status & (Cursor_Status.Enabled | Cursor_Status.Draw)) != 0)
                {
                    DrawPointer(CURSOR[CURSOR_SELECT], blink: ((Cursor_Status & Cursor_Status.Blinking) != 0));
                }
            }
        }

        public virtual void Hide() => Enabled = false;

        public void Init(int count, int depth, IGMDataItem container = null, int? cols = null, int? rows = null)
        {
            if (count <= 0 || depth <= 0)
            {
                if (container == null)
                {
                    Debug.WriteLine($"{this}:: count {count} or depth {depth}, is invalid must be >= 1, or a container {container} must be set instead, Skipping Init()");
                    return;
                }
            }
            else
            {
                SIZE = new Rectangle[count];
                ITEM = new IGMDataItem[count, depth];
                CURSOR = new Point[count];

                Count = (byte)count;
                Depth = (byte)depth;
                BLANKS = new bool[count];
                Descriptions = new Dictionary<int, FF8String>(count);
                this.cols = cols ?? 1;
                this.rows = rows ?? 1;
            }
            if (container != null)
            {
                CONTAINER = container;
            }
            Init();
            ReInit();
            Update();
        }

        /// <summary>
        /// Check inputs
        /// </summary>
        /// <returns>True = input detected</returns>
        public virtual bool Inputs()
        {
            bool ret = false;
            bool mouse = false;

            if ((Cursor_Status & Cursor_Status.Enabled) != 0)
            {
                Cursor_Status &= ~Cursor_Status.Blinking;
                for (int i = 0; i < SIZE.Length; i++)
                {
                    if (SIZE[i].Contains(MouseLocation) && !SIZE[i].IsEmpty && CURSOR[i] != Point.Zero && !BLANKS[i])
                    {
                        CURSOR_SELECT = i;
                        ret = true;
                        mouse = true;
                    }
                }
                if (!ret && (Cursor_Status & Cursor_Status.Horizontal) != 0)
                {
                    if (Input.Button(Buttons.Left))
                    {
                        CURSOR_PREV();
                        ret = true;
                    }
                    else if (Input.Button(Buttons.Right))
                    {
                        CURSOR_NEXT();
                        ret = true;
                    }
                }
                if (!ret && (Cursor_Status & Cursor_Status.Horizontal) == 0 || (Cursor_Status & Cursor_Status.Vertical) != 0)
                {
                    if (Input.Button(Buttons.Up))
                    {
                        CURSOR_PREV();
                        ret = true;
                    }
                    else if (Input.Button(Buttons.Down))
                    {
                        CURSOR_NEXT();
                        ret = true;
                    }
                }
                if (mouse || !ret)
                {
                    if (Input.Button(Buttons.Okay))
                    {
                        Inputs_OKAY();
                        return true;
                    }
                    else if (Input.Button(Buttons.Cancel))
                    {
                        Inputs_CANCEL();
                        return true;
                    }
                    else if (Input.Button(Buttons.Triangle))
                    {
                        Inputs_Triangle();
                        return true;
                    }
                    else if (Input.Button(Buttons.Square))
                    {
                        Inputs_Square();
                        return true;
                    }
                    else if ((Cursor_Status & Cursor_Status.Horizontal) == 0)
                    {
                        if (Input.Button(Buttons.Left))
                        {
                            Inputs_Left();
                            return true;
                        }
                        else if (Input.Button(Buttons.Right))
                        {
                            Inputs_Right();
                            return true;
                        }
                    }
                }
                if (ret && !mouse)
                {
                    Input.ResetInputLimit();
                    if (!skipsnd)
                        init_debugger_Audio.PlaySound(0);
                }
            }
            skipsnd = false;
            return ret;
        }

        public virtual void Inputs_CANCEL()
        {
            Input.ResetInputLimit();
            if (!skipsnd)
                init_debugger_Audio.PlaySound(8);
        }

        public virtual void Inputs_Left()
        {
            Input.ResetInputLimit();
            if (!skipsnd)
                init_debugger_Audio.PlaySound(0);
        }

        public virtual void Inputs_OKAY()
        {
            Input.ResetInputLimit();
            if (!skipsnd)
                init_debugger_Audio.PlaySound(0);
        }

        public virtual void Inputs_Right()
        {
            Input.ResetInputLimit();
            if (!skipsnd)
                init_debugger_Audio.PlaySound(0);
        }

        public virtual void Inputs_Square()
        {
            Input.ResetInputLimit();
            if (!skipsnd)
                init_debugger_Audio.PlaySound(31);
        }

        public virtual void Inputs_Triangle()
        {
            Input.ResetInputLimit();
            if (!skipsnd)
                init_debugger_Audio.PlaySound(0);
        }

        public virtual void ReInit(Characters character, Characters? visablecharacter = null)
        {
            Character = character;
            VisableCharacter = visablecharacter ?? character;
            ReInit();
        }
        /// <summary>
        /// Things that change rarely. Like a party member changes or Laguna dream happens.
        /// </summary>
        public virtual void ReInit()
        {
        }

        public virtual void Show() => Enabled = true;

        /// <summary>
        /// Things that change on every update.
        /// </summary>
        /// <returns>True = signifigant change</returns>
        public virtual bool Update() => false;

        protected int GetCursor_select() => _cursor_select;

        /// <summary>
        /// Things that are fixed values at startup.
        /// </summary>
        protected virtual void Init()
        {
            if (SIZE != null && SIZE.Length > 0)
            {
                for (int i = 0; i < SIZE.Length; i++)
                {
                    int col = (Table_Options & Table_Options.FillRows) != 0 ? i % cols : i / rows;
                    int row = (Table_Options & Table_Options.FillRows) != 0 ? i / cols : i % rows;
                    if (col < cols && row < rows)
                    {
                        if (SIZE[i].IsEmpty) //allows for override a size value before the loop.
                        {
                            SIZE[i] = new Rectangle
                            {
                                X = X + (Width * col) / cols,
                                Y = Y + (Height * row) / rows,
                                Width = Width / cols,
                                Height = Height / rows,
                            };
                        }
                        CURSOR[i] = Point.Zero;
                        InitShift(i, col, row);
                        CURSOR[i].Y += (int)(SIZE[i].Y + 6 * TextScale.Y);
                        CURSOR[i].X += SIZE[i].X;
                    }
                }
            }
            if (SIZE == null || SIZE.Length == 0 || SIZE[0].IsEmpty)
            {
                if (CURSOR == null || CURSOR.Length == 0 || SIZE.Length == 0)
                {
                    CURSOR = new Point[1];
                    SIZE = new Rectangle[1];
                }
                CURSOR[0].Y = (int)(Y + Height / 2 - 6 * TextScale.Y);
                CURSOR[0].X = X;
                SIZE[0] = new Rectangle(X, Y, Width, Height);
            }
        }

        protected virtual void InitShift(int i, int col, int row)
        {
        }

        protected virtual void SetCursor_select(int value) => _cursor_select = value;

        #endregion Methods
    }

}