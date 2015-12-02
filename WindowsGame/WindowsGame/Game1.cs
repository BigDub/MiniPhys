//#define NOT_MY_SHADER

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace WindowsGame
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        /*
         *  IN XNA THE +Z IS COMING OUT OF THE SCREEN.
         *  ^ Y
         *  |
         *  |
         *  |
         *  O---------> X
         *  Z
         */
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Model cubeModel, plane;
        float Elapsed, angle, highestCur, highestEver;
        Matrix viewMatrix, worldMatrix, projectionMatrix;
        Cube[,] cubes;
        Vector2 intersect;
        Texture2D tex, normal, empty, clouds;
        Effect effect;
        Color clear;
        Boolean applyForce = false;
#if !NOT_MY_SHADER
        int renderMode = 0; 
#else
        Vector4[] lightPos;
        float angle = 0;
#endif
        int screenWidth = 800, screenHeight = 600, gridSize = 50;
        SpriteFont spriteFont;

        KeyboardState ks, pks;
        MouseState ms, pms;
        float sensitivity = 500, spd = 10;
        Vector2 camAngle;
        Vector3 camPos, camDir;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = screenWidth;
            graphics.PreferredBackBufferHeight = screenHeight;
            //graphics.IsFullScreen = true;
            graphics.ApplyChanges();
            Window.Title = "MiniPhys";
            base.Initialize();
        }
        
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteFont = Content.Load<SpriteFont>("SpriteFont1");
            tex = Content.Load<Texture2D>("KoalaSQR");
            normal = Content.Load<Texture2D>("crossnrm");
            clouds = Content.Load<Texture2D>("Clouds");
            empty = new Texture2D(GraphicsDevice, 1, 1);
            empty.SetData<Color>(new Color[] { Color.White });
            Material.defaultMaterial = new Material(empty, 20);
            plane = new Model(new Mesh[] { Mesh.createPlane(1,1) });
            Mesh cubeMesh = Mesh.CreateCubeMesh(0.25f);
            cubeModel = new Model(new Mesh[] { cubeMesh });
            initCubes();
            SetUpCamera();
            SetUpShader();
        }

        void initCubes()
        {
            cubes = new Cube[gridSize + 1, gridSize + 1];
            for (int x = 0; x <= gridSize; x++)
            {
                for (int z = 0; z <= gridSize; z++)
                {
                    Cube c = new Cube();
                    c.pos = new Vector3(x - gridSize / 2, 0, z - gridSize / 2);
                    //c.vel = Vector3.UnitY * 5;
                    cubes[x, z] = c;
                }
            }
        }
        void SetUpCamera()
        {
            camPos = new Vector3(30, 20, -30);
            camDir = new Vector3(0,0,-1);
            camAngle = new Vector2(-MathHelper.PiOver4, -MathHelper.PiOver4);
            viewMatrix = Matrix.CreateLookAt(new Vector3(0, 15, 15), new Vector3(0, 5, 0), new Vector3(0, 1, 0));
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1.0f, 200.0f);
            clear = Color.Black;
        }
        void SetUpShader()
        {
#if !NOT_MY_SHADER
            effect = Content.Load<Effect>("Shaders");
            effect.CurrentTechnique = effect.Techniques["Light"];
            effect.Parameters["colorMap"].SetValue(empty);
            effect.Parameters["xDepthMono"].SetValue(new float[] { 1, 1, 1, 1 });
            effect.Parameters["xDepthRange"].SetValue(new float[] { 10, 60, 50 });
            effect.Parameters["xProjection"].SetValue(projectionMatrix);
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["Ambient"].SetValue(new float[] { 0.0f, 0.1f, 0.5f, 1 });
            effect.Parameters["xDc"].SetValue(new float[] { 0.0f, 0.2f, 1.0f, 1 });
            Vector3 dir = new Vector3(1,-1,-1);
            dir.Normalize();
            effect.Parameters["xDd"].SetValue(new float[] { dir.X, dir.Y, dir.Z  });
            effect.Parameters["xCamPos"].SetValue(new float[] { 0, 5, 20 });
            effect.Parameters["shine"].SetValue(100);
#else
            effect = Content.Load<Effect>("RasterTekShader");
            lightPos = new Vector4[4];
            effect.Parameters["projectionMatrix"].SetValue(projectionMatrix);
            effect.Parameters["diffuseColor"].SetValue(new Vector4[] { new Vector4(1,0,0,1), new Vector4(0,1,0,1), new Vector4(0,0,1,1), new Vector4(1) });
            effect.Parameters["shaderTexture"].SetValue(tex);
#endif
        }
        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            Elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            angle += Elapsed;
            intersect = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 12.5f;

            UpdateCubes();
            ManageInput();
#if NOT_MY_SHADER
            angle += MathHelper.PiOver4 * Elapsed;
            for (int i = 0; i < 4; i++)
            {
                lightPos[i] = new Vector4((float)Math.Cos(angle + MathHelper.PiOver2 * i) * 10, 2, (float)Math.Sin(angle + MathHelper.PiOver2 * i) * 10, 0);
            }
            effect.Parameters["lightPosition"].SetValue(lightPos);
#endif
            base.Update(gameTime);
        }

        void ManageInput()
        {
            ManageMouse();
            ManageKeyboard();
        }
        void ManageKeyboard()
        {
            pks = ks;
            ks = Keyboard.GetState();
            #region Keys
            #region Tab
#if !NOT_MY_SHADER
            if (ks.IsKeyDown(Keys.Tab) && pks.IsKeyUp(Keys.Tab))
            {
                renderMode = (renderMode + 1) % 3;
                switch (renderMode)
                {
                    case(0):
                        effect.CurrentTechnique = effect.Techniques["Light"];
                        break;
                    case(1):
                        effect.CurrentTechnique = effect.Techniques["DepthMono"];
                        break;
                    case(2):
                        effect.CurrentTechnique = effect.Techniques["DepthRGB"];
                        break;
                }
            }
#endif
#endregion
            #region Escape
            if (ks.IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }
            #endregion
            #region Space
            if (ks.IsKeyDown(Keys.Space) && !pks.IsKeyDown(Keys.Space))
            {
                applyForce = !applyForce;
            }
            #endregion
            Vector3 movement = new Vector3();
            #region A
            if (ks.IsKeyDown(Keys.A))
            {
                movement.X += 1;
            }
            #endregion
            #region D
            if (ks.IsKeyDown(Keys.D))
            {
                movement.X -= 1;
            }
            #endregion
            #region W
            if (ks.IsKeyDown(Keys.W))
            {
                movement.Z += 1;
            }
            #endregion
            #region S
            if (ks.IsKeyDown(Keys.S))
            {
                movement.Z -= 1;
            }
            #endregion
            #region Q
            if (ks.IsKeyDown(Keys.Q))
            {
                movement.Y += 1;
            }
            #endregion
            #region E
            if (ks.IsKeyDown(Keys.E))
            {
                movement.Y -= 1;
            }
            #endregion
            #endregion
            if (movement != Vector3.Zero)
            {
                movement.Normalize();
                movement = Vector3.Transform(movement, Matrix.CreateFromYawPitchRoll(camAngle.X, -camAngle.Y, 0));
                movement *= spd * Elapsed;
                camPos += movement;
            }
        }
        void ManageMouse()
        {
            pms = ms;
            ms = Mouse.GetState();
            Vector2 mChange = Vector2.Zero;
            if (ms.X != pms.X)
            {
                mChange.X = screenWidth / 2 - ms.X;
                camAngle.X += mChange.X / sensitivity;
            }
            if (ms.Y != pms.Y)
            {
                mChange.Y = screenHeight / 2 - ms.Y;
                camAngle.Y += mChange.Y / sensitivity;
            }
            Mouse.SetPosition(screenWidth / 2, screenHeight / 2);
            camAngle.Y = MathHelper.Clamp(camAngle.Y, -MathHelper.PiOver2 + .01f, MathHelper.PiOver2 - .01f);
            float s = (float)(Math.Cos(camAngle.Y));

            camDir = new Vector3(
                (float)(s * Math.Sin(camAngle.X)),
                (float)(Math.Sin(camAngle.Y)),
                (float)(s * Math.Cos(camAngle.X)));
            camDir.Normalize();
            viewMatrix = Matrix.CreateLookAt(camPos, camPos + camDir, new Vector3(0, 1, 0));
        }
        void UpdateCubes()
        {
            highestCur = 0;
            float mForce = 100, sForce = 50;
            for(int x = 0; x <= gridSize; x++)
            {
                for (int z = 0; z <= gridSize; z++)
                {
                    Cube c = cubes[x, z];
                    float dist = MathHelper.Max(1, 0.5f * (new Vector2(c.pos.X, c.pos.Z) - intersect).Length());
                    c.forces.Y = 0;
                    c.forces.Y += -0.1f;//basic gravity
                    if (applyForce)
                    {
                        c.forces.Y += (mForce / dist);//applied force
                    }
                    c.forces.Y += sForce * -c.pos.Y;
                    /*if (c.pos.Y < 0)
                    {
                        c.forces.Y += 0.1f * -c.pos.Y;//particles beneath pushing up
                    }*/
                    c.vel *= 0.99f;//basic friction
                    #region Neighbors
                    if (x != 0)
                    {
                        dist = cubes[x - 1, z].pos.Y - c.pos.Y;
                        c.forces.Y += dist * sForce;
                    }
                    else
                    {
                        dist = -c.pos.Y;
                        c.forces.Y += dist * sForce;
                    }
                    if (x != gridSize)
                    {
                        dist = cubes[x + 1, z].pos.Y - c.pos.Y;
                        c.forces.Y += dist * sForce;
                    }
                    else
                    {
                        dist = -c.pos.Y;
                        c.forces.Y += dist * sForce;
                    }
                    if (z != 0)
                    {
                        dist = cubes[x, z - 1].pos.Y - c.pos.Y;
                        c.forces.Y += dist * sForce;
                    }
                    else
                    {
                        dist = -c.pos.Y;
                        c.forces.Y += dist * sForce;
                    }
                    if (z != gridSize)
                    {
                        dist = cubes[x, z + 1].pos.Y - c.pos.Y;
                        c.forces.Y += dist * sForce;
                    }
                    else
                    {
                        dist = -c.pos.Y;
                        c.forces.Y += dist * sForce;
                    }
                    #endregion
                }
            }
            for (int x = 0; x <= gridSize; x++)
            {
                for (int z = 0; z <= gridSize; z++)
                {
                    Cube c = cubes[x, z];
                    c.Update(Elapsed);
                    highestCur = (c.pos.Y > highestCur) ? c.pos.Y : highestCur;
                    highestEver = (c.pos.Y > highestEver) ? c.pos.Y : highestEver;
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(clear);
            DepthStencilState depthState = new DepthStencilState();
            depthState.DepthBufferEnable = true; /* Enable the depth buffer */
            depthState.DepthBufferWriteEnable = true; /* When drawing to the screen, write to the depth buffer */
            GraphicsDevice.DepthStencilState = depthState;
#if !NOT_MY_SHADER
            effect.Parameters["xView"].SetValue(viewMatrix);
            effect.Parameters["xCamPos"].SetValue(new float[] {camPos.X, camPos.Y, camPos.Z});
            effect.Parameters["xWorld"].SetValue(Matrix.CreateScale(30));
#else
            effect.Parameters["viewMatrix"].SetValue(viewMatrix);
            effect.Parameters["worldMatrix"].SetValue(Matrix.CreateScale(30));
#endif
            //plane.Draw(GraphicsDevice, effect);

            effect.Parameters["xDc"].SetValue(new float[] { 0.0f, 0.2f, 1.0f, 1 });
            effect.Parameters["Ambient"].SetValue(new float[] { 0.0f, 0.1f, 0.5f, 1 });
            foreach (Cube c in cubes)
            {
                worldMatrix = Matrix.CreateScale(c.scale) * Matrix.CreateFromYawPitchRoll(c.rot.Y, c.rot.X, c.rot.Z) * Matrix.CreateTranslation(c.pos.X, c.pos.Y, c.pos.Z);
#if !NOT_MY_SHADER
                effect.Parameters["xWorld"].SetValue(worldMatrix);
#else
                effect.Parameters["worldMatrix"].SetValue(worldMatrix);
#endif
                cubeModel.Draw(GraphicsDevice, effect);
            }

            effect.Parameters["xDc"].SetValue(new float[] { 1.0f, 0.0f, 0.0f, 1 });
            effect.Parameters["Ambient"].SetValue(new float[] { 0.5f, 0.0f, 0.0f, 1 });
            worldMatrix = Matrix.CreateScale(0.1f) * Matrix.CreateTranslation(intersect.X, MathHelper.Max(5, cubes[(int)intersect.X + (gridSize / 2), (int)intersect.Y + (gridSize / 2)].pos.Y + .6f), intersect.Y);
            effect.Parameters["xWorld"].SetValue(worldMatrix);
            cubeModel.Draw(GraphicsDevice, effect);
            drawHUD();
            base.Draw(gameTime);
        }
        void drawHUD()
        {
            spriteBatch.Begin();
            int level = 0;
            spriteBatch.DrawString(spriteFont, "Applying Force: " + applyForce, new Vector2(1, level + 1), Color.Black);
            spriteBatch.DrawString(spriteFont, "Applying Force: " + applyForce, new Vector2(0, level), Color.White);
            level += 20;
            String s = F.StringVector3(camPos, 3);
            spriteBatch.DrawString(spriteFont, s, new Vector2(1, level + 1), Color.Black);
            spriteBatch.DrawString(spriteFont, s, new Vector2(0, level), Color.White);
            level += 20;
            s = F.StringVector3(camDir, 3);
            spriteBatch.DrawString(spriteFont, s, new Vector2(1, level + 1), Color.Black);
            spriteBatch.DrawString(spriteFont, s, new Vector2(0, level), Color.White);
            level += 20;
            s = "CurHigh: " + F.TrimFloat(highestCur, 3);
            spriteBatch.DrawString(spriteFont, s, new Vector2(1, level + 1), Color.Black);
            spriteBatch.DrawString(spriteFont, s, new Vector2(0, level), Color.White);
            level += 20;
            s = "AllHigh: " + F.TrimFloat(highestEver, 3);
            spriteBatch.DrawString(spriteFont, s, new Vector2(1, level + 1), Color.Black);
            spriteBatch.DrawString(spriteFont, s, new Vector2(0, level), Color.White);
            spriteBatch.End();
        }
    }

    public class Cube
    {
        public Vector3 scale, pos, vel, forces, rot, spin;
        public Cube()
        {
            scale = new Vector3(0.5f);
            pos = Vector3.Zero;
            vel = Vector3.Zero;
            forces = Vector3.Zero;
            rot = Vector3.Zero;
            spin = Vector3.Zero;
        }
        public void Update(float elap)
        {
            vel += forces * elap;
            pos += vel * elap;

            rot += spin * elap;
        }
    }
    public struct CVF //Custom Vertex Format
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Texture;
        public Vector2 NormalMap;
        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(sizeof(float) * 8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1)
        );
        public CVF(Vector3 p, Vector3 n, Vector2 t)
        {
            Position = p;
            Normal = n;
            Texture = t;
            NormalMap = Vector2.Zero;
        }
    }
}
