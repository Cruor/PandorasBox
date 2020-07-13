using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;
using System.Reflection;
using Celeste.Mod.Entities;

namespace Celeste.Mod.PandorasBox
{
    [Tracked(true)]
    public class MarioClearPipeSolid : Solid
    {
        private Color color;

        private Vector2 startNode;
        private Vector2 endNode;
        private Vector2 nextNode;

        private bool startNodeExit;
        private bool endNodeExit;

        private float length;
        private float pipeWidth;

        private string texturePath;

        public MarioClearPipeSolid(Vector2 position, float width, float height, float length, float pipeWidth, string texturePath, int surfaceSound, Vector2 startNode, Vector2 endNode, Vector2 nextNode, bool startNodeExit, bool endNodeExit) : base(position, width, height, true)
        {
            this.startNode = startNode;
            this.endNode = endNode;
            this.nextNode = nextNode;

            this.startNodeExit = startNodeExit;
            this.endNodeExit = endNodeExit;

            this.length = length;
            this.pipeWidth = pipeWidth;

            this.texturePath = texturePath;

            Depth = 10;
            SurfaceSoundIndex = surfaceSound >= 0 ? surfaceSound : 32;

            addPipeVisuals();
        }

        public static MarioClearPipeSolid FromNodes(Vector2 startNode, Vector2 endNode, Vector2 nextNode, bool startNodeExit, bool endNodeExit, float pipeWidth, string texturePath, int surfaceSound)
        {
            MarioClearPipeHelper.Direction direction = MarioClearPipeHelper.GetPipeExitDirection(endNode, startNode);

            float length = (endNode - startNode).Length();

            float halfPipeWidth = pipeWidth / 2f;

            Vector2 widthVector = Vector2.Zero;
            Vector2 lengthVector = Vector2.Zero;

            Vector2 position = startNode;
            float width = 8f;
            float height = 8f;

            if (startNodeExit)
            {
                length += halfPipeWidth;
            }

            if (endNodeExit)
            {
                length -= halfPipeWidth;
            }

            switch (direction)
            {
                case MarioClearPipeHelper.Direction.Up:
                    position = new Vector2(endNode.X - halfPipeWidth, endNode.Y - (endNodeExit ? 0 : halfPipeWidth));
                    width = pipeWidth;
                    height = length;
                    break;

                case MarioClearPipeHelper.Direction.Right:
                    position = new Vector2(startNode.X + (startNodeExit ? 0 : halfPipeWidth), startNode.Y - halfPipeWidth);
                    width = length;
                    height = pipeWidth;
                    break;

                case MarioClearPipeHelper.Direction.Down:
                    position = new Vector2(startNode.X - halfPipeWidth, startNode.Y + (startNodeExit ? 0 : halfPipeWidth));
                    width = pipeWidth;
                    height = length;
                    break;

                case MarioClearPipeHelper.Direction.Left:
                    position = new Vector2(endNode.X - (endNodeExit ? 0 : halfPipeWidth), endNode.Y - halfPipeWidth);
                    width = length;
                    height = pipeWidth;
                    break;

                default:
                    break;
            }

            // Weird offset on non multiple of 16 widths (8, 24, etc)
            if (pipeWidth / 8 % 2 == 1)
            {
                if (endNodeExit && direction == MarioClearPipeHelper.Direction.Right || startNodeExit && direction == MarioClearPipeHelper.Direction.Left)
                {
                    width -= 4f;
                    length -= 4f;
                }
                else if (startNodeExit && direction == MarioClearPipeHelper.Direction.Up || endNodeExit && direction == MarioClearPipeHelper.Direction.Down)
                {
                    height -= 4f;
                    length -= 4f;
                }
            }

            return new MarioClearPipeSolid(position, width, height, length, pipeWidth, texturePath, surfaceSound, startNode, endNode, nextNode, startNodeExit, endNodeExit);
        }

        private static string getCornerType(MarioClearPipeHelper.Direction direction, MarioClearPipeHelper.Direction nextDirection)
        {
            if (direction == MarioClearPipeHelper.Direction.Right && nextDirection == MarioClearPipeHelper.Direction.Up ||
                direction == MarioClearPipeHelper.Direction.Down && nextDirection == MarioClearPipeHelper.Direction.Left)
            {
                return "upLeft";
            } 
            else if (direction == MarioClearPipeHelper.Direction.Down && nextDirection == MarioClearPipeHelper.Direction.Right ||
                direction == MarioClearPipeHelper.Direction.Left && nextDirection == MarioClearPipeHelper.Direction.Up)
            {
                return "upRight";
            }
            else if (direction == MarioClearPipeHelper.Direction.Left && nextDirection == MarioClearPipeHelper.Direction.Down ||
                direction == MarioClearPipeHelper.Direction.Up && nextDirection == MarioClearPipeHelper.Direction.Right)
            {
                return "downRight";
            }
            else if (direction == MarioClearPipeHelper.Direction.Up && nextDirection == MarioClearPipeHelper.Direction.Left ||
                direction == MarioClearPipeHelper.Direction.Right && nextDirection == MarioClearPipeHelper.Direction.Down)
            {
                return "downLeft";
            }
            else
            {
                return "unknown";
            }
        }

        private void addPipeCornerVisuals(MarioClearPipeHelper.Direction direction, MarioClearPipeHelper.Direction nextDirection)
        {
            if (length < pipeWidth)
            {
                return;
            }

            string cornerType = getCornerType(direction, nextDirection);

            if (cornerType != "upLeft" && cornerType != "upRight" && cornerType != "downRight" && cornerType != "downLeft")
            {
                return;
            }

            MTexture nonWallTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/straight_horizontal_middle"]; // TODO - Better texture?
            MTexture innerCornerTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/corner_{cornerType}_inner"];
            MTexture outerCornerTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/corner_{cornerType}_outer"];
            MTexture verticalWallTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/corner_{cornerType}_vertical_wall"];
            MTexture horizontalWallTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/corner_{cornerType}_horizontal_wall"];

            float verticalWallX = -1;
            float horizontalWallY = -1;

            float offsetX = 0;
            float offsetY = 0;

            if (cornerType == "upLeft")
            {
                verticalWallX = pipeWidth - 8;
                horizontalWallY = pipeWidth - 8;
            }
            else if (cornerType == "upRight")
            {
                verticalWallX = 0;
                horizontalWallY = pipeWidth - 8;
            }
            else if (cornerType == "downRight")
            {
                verticalWallX = 0;
                horizontalWallY = 0;
            }
            else if (cornerType == "downLeft")
            {
                verticalWallX = pipeWidth - 8;
                horizontalWallY = 0;
            }

            if (direction == MarioClearPipeHelper.Direction.Right)
            {
                offsetX = length - pipeWidth;
            }
            else if (direction == MarioClearPipeHelper.Direction.Down)
            {
                offsetY = length - pipeWidth;
            }

            for (int x = 0; x < pipeWidth; x += 8)
            {
                for (int y = 0; y < pipeWidth; y += 8)
                {
                    Image image;

                    bool onVertical = x == verticalWallX;
                    bool onHorizontal = y == horizontalWallY;
                    bool innerCorner = x == Math.Abs(verticalWallX - pipeWidth + 8) && y == Math.Abs(horizontalWallY - pipeWidth + 8);

                    if (innerCorner)
                    {
                        image = new Image(innerCornerTexture);
                    }
                    else if (onVertical && onHorizontal)
                    {
                        image = new Image(outerCornerTexture);
                    }
                    else if (onVertical)
                    {
                        image = new Image(verticalWallTexture);
                    }
                    else if (onHorizontal)
                    {
                        image = new Image(horizontalWallTexture);
                    }
                    else
                    {
                        image = new Image(nonWallTexture);
                    }

                    image.X = x + offsetX;
                    image.Y = y + offsetY;

                    Add(image);
                }
            }
        }

        private void addPipeVisuals()
        {
            MarioClearPipeHelper.Direction direction = MarioClearPipeHelper.GetPipeExitDirection(endNode, startNode);
            MarioClearPipeHelper.Direction nextDirection = MarioClearPipeHelper.GetPipeExitDirection(nextNode, endNode);

            bool horizontalPipe = direction == MarioClearPipeHelper.Direction.Right || direction == MarioClearPipeHelper.Direction.Left;
            bool verticalPipe = direction == MarioClearPipeHelper.Direction.Down || direction == MarioClearPipeHelper.Direction.Up;

            float straightLength = !endNodeExit ? length - pipeWidth : length;
            float offset = !endNodeExit && (direction == MarioClearPipeHelper.Direction.Up || direction == MarioClearPipeHelper.Direction.Left) ? pipeWidth : 0;

            if (horizontalPipe)
            {
                for (int x = 0; x < straightLength; x += 8)
                {
                    string columnType = "section";

                    Image top;
                    Image bottom;
                    MTexture middleTexture;

                    if (direction == MarioClearPipeHelper.Direction.Left && x == straightLength - 8 && startNodeExit || direction == MarioClearPipeHelper.Direction.Right && x == straightLength - 8 && endNodeExit)
                    {
                        columnType = "rightExit";
                    }
                    else if (direction == MarioClearPipeHelper.Direction.Left && x == 0 && endNodeExit || direction == MarioClearPipeHelper.Direction.Right && x == 0 && startNodeExit)
                    {
                        columnType = "leftExit";
                    }

                    if (columnType == "rightExit")
                    {
                        top = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_right_top"]);
                        bottom = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_right_bottom"]);

                        middleTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_right_middle"];
                    }
                    else if (columnType == "leftExit")
                    {
                        top = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_left_top"]);
                        bottom = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_left_bottom"]);

                        middleTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_left_middle"];
                    }
                    else
                    {
                        top = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/straight_horizontal_top"]);
                        bottom = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/straight_horizontal_bottom"]);

                        middleTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/straight_horizontal_middle"];
                    }

                    top.X = x + offset;
                    top.Y = 0;

                    bottom.X = x + offset;
                    bottom.Y = pipeWidth - 8;

                    Add(top);
                    Add(bottom);

                    for (int y = 8; y < pipeWidth - 8; y += 8)
                    {
                        Image middle = new Image(middleTexture);

                        middle.X = x + offset;
                        middle.Y = y;

                        Add(middle);
                    }
                }
            }
            else if (verticalPipe)
            {
                for (int y = 0; y < straightLength; y += 8)
                {
                    string rowType = "section";

                    Image left;
                    Image right;
                    MTexture middleTexture;

                    if (direction == MarioClearPipeHelper.Direction.Up && y == straightLength - 8 && startNodeExit || direction == MarioClearPipeHelper.Direction.Down && y == straightLength - 8 && endNodeExit)
                    {
                        rowType = "downExit";
                    }
                    else if (direction == MarioClearPipeHelper.Direction.Up && y == 0 && endNodeExit || direction == MarioClearPipeHelper.Direction.Down && y == 0 && startNodeExit)
                    {
                        rowType = "upExit";
                    }

                    if (rowType == "upExit")
                    {
                        left = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_up_left"]);
                        right = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_up_right"]);

                        middleTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_up_middle"];
                    }
                    else if (rowType == "downExit")
                    {
                        left = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_down_left"]);
                        right = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_down_right"]);

                        middleTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/exit_down_middle"];
                    }
                    else
                    {
                        left = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/straight_vertical_left"]);
                        right = new Image(GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/straight_vertical_right"]);

                        middleTexture = GFX.Game[$"objects/pandorasBox/clearPipe/{texturePath}/straight_vertical_middle"];
                    }

                    left.X = 0;
                    left.Y = y + offset;

                    right.X = pipeWidth - 8;
                    right.Y = y + offset;

                    Add(left);
                    Add(right);

                    for (int x = 8; x < pipeWidth - 8; x += 8)
                    {
                        Image middle = new Image(middleTexture);

                        middle.X = x;
                        middle.Y = y + offset;

                        Add(middle);
                    }
                }
            }

            if (!endNodeExit)
            {
                addPipeCornerVisuals(direction, nextDirection);
            }
        }
    }
}
