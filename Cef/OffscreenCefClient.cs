﻿using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using UnityEngine;
using Xilium.CefGlue;

namespace StationeersWebDisplay.Cef
{
    internal class OffscreenCefClient : CefClient
    {
        private readonly DialogHandler _dialogHandler = new();
        private readonly DownloadHandler _downloadHandler = new();
        private readonly LifeSpanHandler _lifespanHandler;
        private readonly RenderHandler _renderHandler;

        private readonly object PixelLock = new object();
        private byte[] _pixelBuffer;

        private CefBrowserHost _host;

        private Vector2? _lastMousePos = null;

        public OffscreenCefClient(Size windowSize)
        {
            this._pixelBuffer = new byte[windowSize.Width * windowSize.Height * 4];
            this._lifespanHandler = new(this);
            this._renderHandler = new(windowSize.Width, windowSize.Height, this);
        }

        private string _pendingUrl = null;
        public string Url
        {
            get
            {
                if (this._host == null)
                {
                    return this._pendingUrl;
                }

                return this._host.GetBrowser().GetMainFrame().Url;
            }

            set
            {
                if (this._host == null)
                {
                    Logging.LogTrace("URL set before host is ready.  Setting to pending");
                    this._pendingUrl = value;
                    return;
                }

                this._host.GetBrowser().GetMainFrame().LoadUrl(value);
            }
        }

        public void CopyToTexture(Texture2D pTexture)
        {
            lock (this.PixelLock)
            {
                pTexture.LoadRawTextureData(this._pixelBuffer);
                pTexture.Apply(false);
            }
        }

        public void MouseMove(Vector2 position)
        {
            this._lastMousePos = position;
            this._host.SendMouseMoveEvent(new CefMouseEvent((int)position.x, (int)position.y, CefEventFlags.None), false);
        }

        public void MouseOut()
        {
            if (this._lastMousePos == null)
            {
                return;
            }

            var position = this._lastMousePos.Value;
            this._host.SendMouseMoveEvent(new CefMouseEvent((int)position.x, (int)position.y, CefEventFlags.None), true);
            this._lastMousePos = null;
        }

        public void Shutdown()
        {
            if (this._host != null)
            {
                this._host.CloseBrowser(true);
                this._host.Dispose();
                this._host = null;
            }
        }


        protected override CefLoadHandler GetLoadHandler()
        {
            return base.GetLoadHandler();
        }

        protected override CefDialogHandler GetDialogHandler()
        {
            return this._dialogHandler;
        }

        protected override CefDownloadHandler GetDownloadHandler()
        {
            return this._downloadHandler;
        }

        protected override CefLifeSpanHandler GetLifeSpanHandler()
        {
            return this._lifespanHandler;
        }

        protected override CefRenderHandler GetRenderHandler()
        {
            return this._renderHandler;
        }

        private void _TrySetHost(CefBrowserHost host)
        {
            this._host = host;
            Logging.LogTrace("TrySetHost");
            if (this._pendingUrl != null)
            {
                Logging.LogTrace("Host set with a pending url.  Re-navigating.");
                this._host.GetBrowser().GetMainFrame().LoadUrl(this._pendingUrl);
                this._pendingUrl = null;
            }
        }

        // TODO: Request handler, only allow requests to whitelisted sites / the api.

        private class LoadHandler : CefLoadHandler
        {
            private readonly OffscreenCefClient client;
            public LoadHandler(OffscreenCefClient client) {
                this.client = client;
            }

            protected override void OnLoadStart(CefBrowser browser, CefFrame frame, CefTransitionType transitionType)
            {
                base.OnLoadStart(browser, frame, transitionType);
            }

            protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
            {
                base.OnLoadEnd(browser, frame, httpStatusCode);
            }
        }

        private class DialogHandler : CefDialogHandler
        {
            protected override bool OnFileDialog(CefBrowser browser, CefFileDialogMode mode, string title, string defaultFilePath, string[] acceptFilters, int selectedAcceptFilter, CefFileDialogCallback callback)
            {
                return false;
            }
        }

        private class DownloadHandler : CefDownloadHandler
        {
            protected override void OnBeforeDownload(CefBrowser browser, CefDownloadItem downloadItem, string suggestedName, CefBeforeDownloadCallback callback)
            {
                // TODO: How do we cancel, just ignore?
            }
        }

        private class LifeSpanHandler : CefLifeSpanHandler
        {
            private readonly OffscreenCefClient client;
            public LifeSpanHandler(OffscreenCefClient client)
            {
                this.client = client;
            }

            protected override void OnAfterCreated(CefBrowser browser)
            {
                this.client._TrySetHost(browser.GetHost());
                base.OnAfterCreated(browser);
            }
            protected override bool OnBeforePopup(CefBrowser browser, CefFrame frame, string targetUrl, string targetFrameName, CefWindowOpenDisposition targetDisposition, bool userGesture, CefPopupFeatures popupFeatures, CefWindowInfo windowInfo, ref CefClient client, CefBrowserSettings settings, ref CefDictionaryValue extraInfo, ref bool noJavascriptAccess)
            {
                return false;
            }
        }

        internal class RenderHandler : CefRenderHandler
        {
            private readonly AccessibilityHandler _accessibilityHandler = new();

            private readonly OffscreenCefClient client;

            private readonly int _windowWidth;
            private readonly int _windowHeight;

            public RenderHandler(int windowWidth, int windowHeight, OffscreenCefClient client)
            {
                this._windowWidth = windowWidth;
                this._windowHeight = windowHeight;
                this.client = client;
            }

            protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect)
            {
                rect.X = 0;
                rect.Y = 0;
                rect.Width = this._windowWidth;
                rect.Height = this._windowHeight;
                return true;
            }

            protected override void GetViewRect(CefBrowser browser, out CefRectangle rect)
            {
                rect = new();
                rect.X = 0;
                rect.Y = 0;
                rect.Width = this._windowWidth;
                rect.Height = this._windowHeight;
            }

            protected override bool GetScreenPoint(CefBrowser browser, int viewX, int viewY, ref int screenX, ref int screenY)
            {
                screenX = viewX;
                screenY = viewY;
                return true;
            }

            [SecurityCritical]
            protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
            {
                if (browser != null)
                {
                    lock (client.PixelLock)
                    {
                        if (browser != null)
                            Marshal.Copy(buffer, this.client._pixelBuffer, 0, this.client._pixelBuffer.Length);
                    }
                }
            }

            protected override void OnAcceleratedPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr sharedHandle)
            {
            }

            protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
            {
                return false;
            }

            protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
            {
            }

            protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y)
            {
            }

            protected override void OnImeCompositionRangeChanged(CefBrowser browser, CefRange selectedRange, CefRectangle[] characterBounds)
            {
            }

            protected override CefAccessibilityHandler GetAccessibilityHandler()
            {
                return this._accessibilityHandler;
            }
        }

        private class AccessibilityHandler : CefAccessibilityHandler
        {
            protected override void OnAccessibilityLocationChange(CefValue value)
            {
            }

            protected override void OnAccessibilityTreeChange(CefValue value)
            {
            }
        }
    }
}
