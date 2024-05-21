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

        [NonSerialized]
        private string _url = "about:blank";

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
                    Logging.LogTrace($"Changing WebDisplayBehavior url to {value}");
                    this._browserClient.Url = value;
                }
                else
                {
                    Logging.LogTrace($"Deferring WebDisplayBehavior url {value} since browser is not yet loaded.");
                }
            }
        }

        void Awake()
        {
            Logging.LogTrace($"Creating WebDisplayBehavior with url {this._url}");

            this._browserTexture = new Texture2D(this.Resolution.Width, this.Resolution.Height, TextureFormat.BGRA32, false);

            // Weird jank to set up a shader through code.
            this._renderMaterial = new Material(Shader.Find("Standard"));
            this._renderMaterial.SetTexture("_MainTex", this._browserTexture);
            this._renderMaterial.mainTexture = this._browserTexture;
            this._renderMaterial.mainTextureScale = new Vector2(1, -1);

            this._browserClient = CefHost.CreateClient(this._url, this.Resolution);
        }

        void Update()
        {
            // This is a bit aggressive doing this on every frame, but it seems Awake might be too early
            // for some configurations?
            this.Renderer.material = this._renderMaterial;

            this._browserClient.CopyToTexture(this._browserTexture);

            this.UpdateCursor();
        }

        void OnDestroy()
        {
            Logging.LogTrace("Destroying WebDisplayBehavior");
            this._browserClient.Shutdown();
            this._browserClient = null;
        }

        private void UpdateCursor()
        {
            var collider = this.CursorCollider;
            if (this.CursorCollider == null)
            {
                return;
            }

            var ray = new Ray(Camera.main.ScreenToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f)), Camera.main.transform.forward);
            if (!collider.Raycast(ray, out var hitInfo, this.CursorInteractDistance))
            {
                this._browserClient.MouseOut();
                return;
            }

            var intersectionPoint = new Plane(this.transform.forward, this.transform.position).ClosestPointOnPlane(hitInfo.point);

            // TODO: Adjust for rotation.
            var colliderBounds = collider.bounds;
            var cursorPos = new Vector2(
                1 - (intersectionPoint.x - colliderBounds.min.x) / (colliderBounds.max.x - colliderBounds.min.x),
                1 - (intersectionPoint.y - colliderBounds.min.y) / (colliderBounds.max.y - colliderBounds.min.y)
            );

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
                // This is a no-op if the mouse was not being tracked by thw browser.
                this._browserClient.MouseOut();

                this._mouseDown = UnityEngine.Input.GetMouseButton(0);
            }
        }
    }
}
