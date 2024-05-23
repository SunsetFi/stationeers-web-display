using StationeersWebDisplay.Cef;
using System;
using System.Drawing;
using UnityEngine;

namespace StationeersWebDisplay
{
    public class WebDisplayBehavior : MonoBehaviour
    {

        [NonSerialized]
        private Texture2D _browserTexture;

        [NonSerialized]
        private Texture2D _disabledTexture;


        [NonSerialized]
        private Material _renderMaterial;

        [NonSerialized]
        private OffscreenCefClient _browserClient;

        [NonSerialized]
        private bool _mouseDown = false;
        [NonSerialized]
        private bool _trackingMouse = false;

        public Size Resolution = new Size(1024, 1024);

        public MeshRenderer Renderer;

        public Collider CursorCollider;
        public float CursorInteractDistance = 2.5f;

        private bool _enabled = true;
        public bool StartEnabled = true;

        public bool Enabled
        {
            get
            {
                return this._enabled;
            }
            set
            {
                if (this._enabled == value)
                {
                    return;
                }

                if (value)
                {
                    this.Enable();
                }
                else
                {
                    this.Disable();
                }
            }
        }

        private string _url = "about:blank";
        public string InitialUrl = "about:blank";

        public string Url
        {
            get
            {
                return this._browserClient?.Url ?? this._url;
            }
            set
            {
                this._url = value;
                if (this._browserClient != null)
                {
                    this._browserClient.Url = value;
                }
            }
        }

        public void Enable()
        {
            if (this._browserClient != null)
            {
                return;
            }

            this._enabled = true;
            this._browserClient = CefHost.CreateClient(this._url, this.Resolution);

            this._renderMaterial.SetTexture("_MainTex", this._browserTexture);

            this._renderMaterial.EnableKeyword("_EMISSION");
            this._renderMaterial.SetTexture("_EmissionMap", this._browserTexture);
            this._renderMaterial.SetColor("_EmissionColor", UnityEngine.Color.white * Mathf.LinearToGammaSpace(0.75f));
        }

        public void Disable()
        {
            if (this._browserClient == null)
            {
                return;
            }

            this._enabled = false;
            this._browserClient.Shutdown();
            this._browserClient = null;

            this._renderMaterial.SetTexture("_MainTex", _disabledTexture);

            // This is taking a lot of effort to turn off without getting a white screen...
            this._renderMaterial.DisableKeyword("_EMISSION");
            this._renderMaterial.SetTexture("_EmissionMap", _disabledTexture);
            this._renderMaterial.SetColor("_EmissionColor", UnityEngine.Color.black);
        }

        void Awake()
        {
            if (!string.IsNullOrEmpty(this.InitialUrl))
            {
                this._url = this.InitialUrl;
            }

            this._enabled = this.StartEnabled;

            this._browserTexture = new Texture2D(this.Resolution.Width, this.Resolution.Height, TextureFormat.BGRA32, false);

            this._disabledTexture = new Texture2D(1, 1, TextureFormat.BGRA32, false);
            this._disabledTexture.SetPixel(0, 0, UnityEngine.Color.black);

            this._renderMaterial = new Material(Shader.Find("Standard"));

            // Unity has an inverted Y axis compared to the browser renderer.
            this._renderMaterial.SetTextureScale("_MainTex", new Vector2(1, -1));
            this._renderMaterial.SetTextureScale("_EmissionMap", new Vector2(1, -1));

            if (this._enabled)
            {
                this.Enable();
            }
            else
            {
                this.Disable();
            }
        }

        void Update()
        {
            if (this._browserClient != null)
            {
                this.UpdateScreen();
                this.UpdateCursor();
            }
        }

        void OnDestroy()
        {
            this._browserClient.Shutdown();
            this._browserClient = null;
        }

        private void UpdateScreen()
        {
            this._browserClient.CopyToTexture(this._browserTexture);

            // This is a bit aggressive doing this on every frame, but it seems Awake might be too early
            // for some configurations?
            if (this.Renderer)
            {
                this.Renderer.material = this._renderMaterial;
            }
        }

        private void UpdateCursor()
        {
            var collider = this.CursorCollider;
            if (collider == null)
            {
                return;
            }

            var ray = new Ray(Camera.main.ScreenToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f)), Camera.main.transform.forward);
            if (!collider.Raycast(ray, out var hitInfo, this.CursorInteractDistance))
            {
                this._browserClient.MouseOut();
                return;
            }

            var screenPlane = new Plane(this.transform.forward, this.transform.position);
            var intersectionPoint = screenPlane.ClosestPointOnPlane(hitInfo.point);
            // Transform the hit point to the local space of the screen object
            var localHitPoint = this.transform.InverseTransformPoint(intersectionPoint);

            var colliderBounds = collider.bounds;
            // Apply the transformation of the collider to the intersection point to get the local coordinates.
            var localBounds = new Bounds(this.transform.InverseTransformPoint(colliderBounds.center),
                                         this.transform.InverseTransformVector(colliderBounds.size));


            var cursorPos = new Vector2(
                (localHitPoint.x - localBounds.min.x) / (localBounds.max.x - localBounds.min.x),
                1 - (localHitPoint.y - localBounds.min.y) / (localBounds.max.y - localBounds.min.y)
            );

            // Not sure why this is an issue or what the math behind this is, but we
            // have a flipped axis on certain rotations
            // Yes, this happens on two different axes.  Its baffling.
            var eulerAngles = this.transform.rotation.eulerAngles;
            if (eulerAngles.y == 0 || eulerAngles.y == 270)
            {
                cursorPos.x = 1 - cursorPos.x;
            }

            // Things get even weirder when rotated about the x or z axes...
            // Not bothering with that for now.

            // if (UnityEngine.Input.GetMouseButton(0))
            // {
            //     Logging.LogTrace($"intersection at {intersectionPoint.x} {intersectionPoint.y}, mins {colliderBounds.min.x} {colliderBounds.min.y} {colliderBounds.min.z} maxes {colliderBounds.max.x} {colliderBounds.max.y} {colliderBounds.max.z} cursor pos at {cursorPos.x} {cursorPos.y}");
            // }

            if (cursorPos.x >= 0 && cursorPos.x <= 1 && cursorPos.y >= 0 && cursorPos.y <= 1)
            {
                var browserPos = new Vector2(cursorPos.x * this.Resolution.Width, cursorPos.y * this.Resolution.Height);
                this._browserClient.MouseMove(browserPos);
                if (UnityEngine.Input.GetMouseButton(0))
                {
                    if (!this._mouseDown)
                    {
                        this._mouseDown = true;
                        this._trackingMouse = true;
                        this._browserClient.MouseDown(browserPos);
                    }
                }
                else if (this._mouseDown && this._trackingMouse)
                {
                    // Only send this if we have affirmatively tracked the click on our own screen.
                    // We do not want to send mouse up if the user holds the mouse button then cursors onto us.
                    this._mouseDown = false;
                    this._trackingMouse = false;
                    this._browserClient.MouseUp(browserPos);
                }
            }
            else
            {
                // This is a no-op if the mouse was not being tracked by the browser.
                this._browserClient.MouseOut();

                this._mouseDown = UnityEngine.Input.GetMouseButton(0);
            }
        }
    }
}
