using Objects.Rockets.Log;
using Objects.Rockets.Log.RocketEvents;
using StationeersWebDisplay.Cef;
using System.Drawing;
using UnityEngine;
using UnityEngine.Windows;

namespace StationeersWebDisplay
{
    internal class WebDisplayBehavior : MonoBehaviour
    {
        private readonly Size _browserSize = new Size(1024, 768);
        private readonly Texture2D _browserTexture = new Texture2D(1024, 768, TextureFormat.BGRA32, false);

        private OffscreenCefClient _browserClient;

        private Collider _collider;

        private Material _renderMaterial;
        public Material RenderMaterial
        {
            get
            {
                return this._renderMaterial;
            }
            set
            {
                this._renderMaterial = value;
                this._renderMaterial.mainTexture = this._browserTexture;
                this._renderMaterial.mainTextureScale = new Vector2(1, -1);
            }
        }

        public Size Bezel = new(0, 0);

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
            this._browserClient = CefHost.CreateClient(this._url, this._browserSize);
            this._collider = this.gameObject.GetComponent<Collider>();
            if (this._collider == null)
            {
                Logging.LogError("Unable to find collider for WebDisplayBehavior.  Mouse interactivity will be disabled.");
            }
        }

        void Update()
        {
            // This is for the picture frame hyjack.  Do not use on final.
            this._renderMaterial.mainTexture = this._browserTexture;
            this._renderMaterial.mainTextureScale = new Vector2(1, -1);
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
            if (this._collider == null)
            {
                return;
            }

            var ray = new Ray(Camera.main.ScreenToWorldPoint(new Vector3(0.5f, 0.5f, 0.0f)), Camera.main.transform.forward);
            this._collider.Raycast(ray, out var hitInfo, 1f);

            var vector3 = new Plane(this.transform.forward, this.transform.position).ClosestPointOnPlane(hitInfo.point) - this.transform.position;
            var localScale = this.transform.localScale;
            var cursorPos = new Vector2(
                0.5f + (vector3.x - this.Bezel.Width) / (localScale.x - this.Bezel.Width * 2),
                0.5f - (vector3.y - this.Bezel.Height) / (localScale.y - this.Bezel.Height * 2)
            );

            var browserPos = new Vector2(cursorPos.x * this._browserSize.Width, cursorPos.y * this._browserSize.Height);
            if (UnityEngine.Input.GetMouseButton(0))
            {
                Logging.LogTrace($"Mouse offset {vector3.x} {vector3.y} scale {localScale.x} {localScale.y} cursorPos {cursorPos.x} {cursorPos.y} - browserPos {browserPos.x} {browserPos.y}");
            }

            if (cursorPos.x >= 0 && cursorPos.x <= 1 && cursorPos.y >= 0 && cursorPos.y <= 1)
            {
                this._browserClient.MouseMove(browserPos);
            }
            else
            {
                this._browserClient.MouseOut();
            }
        }
    }
}
