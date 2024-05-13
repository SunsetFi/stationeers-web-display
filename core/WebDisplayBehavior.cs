﻿using StationeersWebDisplay.Cef;
using System.Drawing;
using UnityEngine;

namespace StationeersWebDisplay
{
    internal class WebDisplayBehavior : MonoBehaviour
    {
        private readonly Size _browserSize = new Size(1024, 768);
        private Texture2D _browserTexture;
        private OffscreenCefClient _browserClient;
        private bool _mouseDown = false;
        private bool _trackingMouse = false;

        public MeshRenderer Renderer;
        public Collider CursorCollider;
        public Vector2 Bezel = new(0, 0);

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

            this._browserTexture = new Texture2D(1024, 768, TextureFormat.BGRA32, false);

            this.Renderer.material = new Material(Shader.Find("Default-Material"));
            this.Renderer.material.color = UnityEngine.Color.black;
            this.Renderer.material.mainTexture = this._browserTexture;
            // Unity texture coords are flipped, so flip it back to match the browser.
            this.Renderer.material.mainTextureScale = new Vector2(1, -1);

            this._browserClient = CefHost.CreateClient(this._url, this._browserSize);
        }

        void Update()
        {
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
            if (!collider.Raycast(ray, out var hitInfo, 1f))
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

            // Apply bezel percentage
            cursorPos.x = (cursorPos.x - this.Bezel.x) / (1 - 2 * this.Bezel.x);
            cursorPos.y = (cursorPos.y - this.Bezel.y) / (1 - 2 * this.Bezel.y);

            if (UnityEngine.Input.GetMouseButton(0))
            {
                Logging.LogTrace($"intersection at {intersectionPoint.x} {intersectionPoint.y}, mins {colliderBounds.min.x} {colliderBounds.min.y} {colliderBounds.min.z} maxes {colliderBounds.max.x} {colliderBounds.max.y} {colliderBounds.max.z} cursor pos at {cursorPos.x} {cursorPos.y}");
            }

            if (cursorPos.x >= 0 && cursorPos.x <= 1 && cursorPos.y >= 0 && cursorPos.y <= 1)
            {
                var browserPos = new Vector2(cursorPos.x * this._browserSize.Width, cursorPos.y * this._browserSize.Height);
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
                this._browserClient.MouseOut();
                this._mouseDown = UnityEngine.Input.GetMouseButton(0);
            }
        }
    }
}