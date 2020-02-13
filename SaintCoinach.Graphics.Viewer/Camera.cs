using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Keys = System.Windows.Forms.Keys;

namespace SaintCoinach.Graphics.Viewer {
    using SharpDX;

    public class Camera : IUpdateableComponent {
        #region Fields
        private Vector3 _CameraPosition = Vector3.UnitX;
        private Vector3 _Up = Vector3.Up;
        private Vector3 _OrthoLookAt = Vector3.Zero;

        private float _Yaw = 0;
        private float _Pitch = 0;
        private Matrix _Projection;
        private Matrix _View;
        private float _FoV = 0.9f;
        private float _OrthoScale = 1.0f;
        private bool _OrthoMode = false;
        private bool _OKeyWasDown = false;

        const float RotationSpeed = (float)(Math.PI / 2f);
        const float MoveSpeed = 20.0f;
        const float MouseRotationSpeedYaw = RotationSpeed / 500f;
        const float MouseRotationSpeedPitch = RotationSpeed / 300f;
        const float MouseOrthoPanSpeed = 0.85f;
        const float MouseOrthoZoomSpeed = 0.1f;

        private Engine _Engine;

        private MouseState _PreviousMouseState;
        private MouseState _CurrentMouseState;
        #endregion

        #region Properties
        public Matrix Projection { get { return _Projection; } }
        public Matrix View { get { return _View; } }
        public Vector3 CameraPosition {
            get { return _CameraPosition; }
            set { _CameraPosition = value; }
        }
        public Vector3 OrthoLookAt {
            get { return _OrthoLookAt; }
            set { _OrthoLookAt = value; }
        }
        public float Yaw {
            get { return _Yaw; }
            set { _Yaw = value; }
        }
        public float Pitch {
            get { return _Pitch; }
            set { _Pitch = value; }
        }
        public float FoV {
            get { return _FoV; }
            set { _FoV = value; }
        }
        public float OrthoScale {
            get { return _OrthoScale; }
            set { _OrthoScale = value; }
        }
        public bool OrthoMode {
            get { return _OrthoMode; }
            set { _OrthoMode = value; }
        }
        public bool OKeyWasDown {
            get { return _OKeyWasDown; }
            set { _OKeyWasDown = value; }
        }
        #endregion

        #region Constructor
        public Camera(Engine engine) {
            _Engine = engine;

            Reset();
        }
        #endregion

        #region Control
        public void Reset() {
            _CameraPosition = Vector3.Zero + 2f * Vector3.BackwardRH + 1f * Vector3.Up;
            _Yaw = 0;
            _Pitch = 0;

            _OrthoLookAt = Vector3.Zero;
            OrthoScale = 1.0f;

            UpdateViewMatrix();
        }
        public Matrix GetRotation() {
            return Matrix.RotationYawPitchRoll(_Yaw, _Pitch, 0);
        }
        private void UpdateViewMatrix() {
            var rotation = GetRotation();

            Vector3 cameraOriginalTarget = new Vector3(0, 0, -1);
            Vector3 cameraRotatedTarget = (Vector3)Vector3.Transform(cameraOriginalTarget, rotation);
            Vector3 cameraFinalTarget = _CameraPosition + cameraRotatedTarget;

            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);
            Vector3 cameraRotatedUpVector = (Vector3)Vector3.Transform(cameraOriginalUpVector, rotation);

            if( OrthoMode ) {
                Vector3 orthoCamera = _OrthoLookAt;
                Vector3 orthoCameraUp = new Vector3( 0, 0, -1 ); ;
                orthoCamera.Y += 10;    //***** TODO: Need to make this adjustable because not everything is at zero dumbass. *****
                _View = Matrix.LookAtRH( orthoCamera, _OrthoLookAt, orthoCameraUp );
            }
            else{
                _View = Matrix.LookAtRH( _CameraPosition, cameraFinalTarget, cameraRotatedUpVector );
            }
        }
        public void AddToCameraPosition(Vector3 vectorToAdd) {
            var rotation = GetRotation();

            Vector3 rotatedVector = (Vector3)Vector3.Transform(vectorToAdd, rotation);
            _CameraPosition += MoveSpeed * rotatedVector;
        }
        #endregion

        #region Visibility test
        public bool Contains(BoundingBox bbox) {
            var frustum = new BoundingFrustum(_View * _Projection);
            return frustum.Contains(bbox) != ContainmentType.Disjoint;
        }
        public bool IsVisible(BoundingBox bounds) {
            return IsVisible(bounds.Minimum) || IsVisible(bounds.Maximum);
        }
        public bool IsVisible(Vector3 point) {
            return false;
        }
        public bool IsVisible(Vector4 point) {
            return IsVisible(new Vector3(point.X, point.Y, point.Z));
        }
        #endregion

        private bool _IsEnabled = true;
        public bool IsEnabled { get { return _IsEnabled; } set { _IsEnabled = value; } }

        public void Update(EngineTime time) {
            _PreviousMouseState = _CurrentMouseState;
            _CurrentMouseState = _Engine.Mouse.GetState();

            var amount = (float)(time.ElapsedTime.TotalMilliseconds / 2000f);
            Vector3 moveVector = new Vector3(0, 0, 0);
            var aspectRatio = (float)(_Engine.ViewportSize.Width / (float)_Engine.ViewportSize.Height);

                //  Toggle orthogonal mode if we want.
                if( _Engine.IsActive )
                {
                if( _Engine.Keyboard.IsKeyDown( Keys.O ) ) {
                    if( !OKeyWasDown ) OrthoMode = !OrthoMode;
                    OKeyWasDown = true;
                }
                else {
                    OKeyWasDown = false;
                }
                }

                if (_Engine.IsActive) {
                var modFactor = 2f;
                if (_Engine.Keyboard.IsKeyDown(Keys.Space))
                    modFactor *= 10f;
                if (_Engine.Keyboard.IsKeyDown(Keys.ShiftKey))
                    amount *= modFactor;
                if (_Engine.Keyboard.IsKeyDown(Keys.ControlKey))
                    amount /= modFactor;

                if (_Engine.Keyboard.IsKeyDown(Keys.W))
                    moveVector += new Vector3(0, 0, -1);
                if (_Engine.Keyboard.IsKeyDown(Keys.S))
                    moveVector += new Vector3(0, 0, 1);
                if (_Engine.Keyboard.IsKeyDown(Keys.D))
                    moveVector += new Vector3(1, 0, 0);
                if (_Engine.Keyboard.IsKeyDown(Keys.A))
                    moveVector += new Vector3(-1, 0, 0);
                if (_Engine.Keyboard.IsKeyDown(Keys.Q))
                    moveVector += new Vector3(0, 1, 0);
                if (_Engine.Keyboard.IsKeyDown(Keys.Z))
                    moveVector += new Vector3(0, -1, 0);
                if (_Engine.Keyboard.IsKeyDown(Keys.R))
                    Reset();

                if( OrthoMode ) {
                    if( _Engine.Keyboard.IsKeyDown( Keys.Q ) )
                        OrthoScale *= 1.0f + 0.01f * amount;
                    if( _Engine.Keyboard.IsKeyDown( Keys.Z ) )
                        OrthoScale *= 1.0f - 0.01f * amount;
                    if( _Engine.Keyboard.IsKeyDown( Keys.W ) )
                        _OrthoLookAt.Z -= amount * 10.0f;
                    if( _Engine.Keyboard.IsKeyDown( Keys.S ) )
                        _OrthoLookAt.Z += amount * 10.0f;
                    if( _Engine.Keyboard.IsKeyDown( Keys.D ) )
                        _OrthoLookAt.X += amount * 10.0f;
                    if( _Engine.Keyboard.IsKeyDown( Keys.A ) )
                        _OrthoLookAt.X -= amount * 10.0f;
                }

                if (_Engine.Keyboard.IsKeyDown(Keys.Left))
                    _Yaw += RotationSpeed * amount * 2;
                if (_Engine.Keyboard.IsKeyDown(Keys.Right))
                    _Yaw -= RotationSpeed * amount * 2;

                if (_Engine.Keyboard.IsKeyDown(Keys.Up))
                    _Pitch += RotationSpeed * amount * 2;
                if (_Engine.Keyboard.IsKeyDown(Keys.Down))
                    _Pitch -= RotationSpeed * amount * 2;

                if (_CurrentMouseState.LeftButton) {
                    if (_CurrentMouseState.RightButton)
                        moveVector += new Vector3(0, 0, -1);
                    var mouseMove = _CurrentMouseState.AbsolutePosition - _PreviousMouseState.AbsolutePosition;
                    _Yaw -= mouseMove.X * MouseRotationSpeedYaw;
                    _Pitch -= mouseMove.Y * MouseRotationSpeedPitch;

                    if( OrthoMode ){
                        _OrthoLookAt.X -= mouseMove.X * MouseOrthoPanSpeed * OrthoScale;
                        _OrthoLookAt.Z -= mouseMove.Y * MouseOrthoPanSpeed * OrthoScale;
                    }
                }

                float scrollAmount = (float)System.Math.Sign( _CurrentMouseState.MouseWheelDelta );

                if( OrthoMode ) {
                    OrthoScale *= 1.0f - ( 0.1f * scrollAmount * MouseOrthoZoomSpeed );
                }

                AddToCameraPosition(moveVector * amount);
            }

            UpdateViewMatrix();

            float orthoAspectRatio = (float)_Engine.ViewportSize.Width / (float)_Engine.ViewportSize.Height;

            if( OrthoMode ) {
                _Projection = Matrix.OrthoRH( OrthoScale * 1000.0f * orthoAspectRatio, OrthoScale * 1000.0f, 0.1f, 100000.0f );
            }
            else {
                _Projection = Matrix.PerspectiveFovRH( FoV, aspectRatio, 0.1f, 10000.0f );
            }
        }
    }
}
