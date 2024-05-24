using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using UnityEngine;
using Xilium.CefGlue;

namespace StationeersWebDisplay.Cef
{
    public class StationeersCefClient : CefClient
    {
        private readonly ICollection<Uri> _allowedUrls;

        private readonly DialogHandler _dialogHandler = new();
        private readonly DownloadHandler _downloadHandler = new();
        private readonly LifeSpanHandler _lifespanHandler;
        private readonly RenderHandler _renderHandler;
        private readonly RequestHandler _requestHandler;

        private readonly object _pixelLock = new object();
        private byte[] _pixelBuffer;
        private volatile bool _pixelBufferInvalidated = false;

        private CefBrowserHost _host;

        private Vector2? _lastMousePos = null;

        public StationeersCefClient(Size windowSize, ICollection<Uri> allowedUrls)
        {
            this._allowedUrls = allowedUrls;

            this._pixelBuffer = new byte[windowSize.Width * windowSize.Height * 4];
            this._lifespanHandler = new(this);
            this._renderHandler = new(windowSize.Width, windowSize.Height, this);
            this._requestHandler = new(this);
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
                    this._pendingUrl = value;
                    return;
                }

                this._host.GetBrowser().GetMainFrame().LoadUrl(value);
            }
        }

        public void CopyToTexture(Texture2D pTexture)
        {
            lock (this._pixelLock)
            {
                pTexture.LoadRawTextureData(this._pixelBuffer);
                pTexture.Apply(false);
            }
        }

        public void CopyToTextureIfChanged(Texture2D pTexture)
        {
            if (this._pixelBufferInvalidated)
            {
                lock (this._pixelLock)
                {
                    this.CopyToTexture(pTexture);
                    this._pixelBufferInvalidated = false;
                }
            }
        }

        public void MouseMove(Vector2 position)
        {
            this._lastMousePos = position;
            this._host.SendMouseMoveEvent(new CefMouseEvent((int)position.x, (int)position.y, CefEventFlags.None), false);
        }

        public void MouseDown(Vector2 position)
        {
            this._lastMousePos = position;
            this._host.SendMouseClickEvent(new CefMouseEvent((int)position.x, (int)position.y, CefEventFlags.None), CefMouseButtonType.Left, false, 1);
        }

        public void MouseUp(Vector2 position)
        {
            this._host.SendMouseClickEvent(new CefMouseEvent((int)position.x, (int)position.y, CefEventFlags.None), CefMouseButtonType.Left, true, 1);
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

        protected override CefRequestHandler GetRequestHandler()
        {
            return this._requestHandler;
        }

        protected override CefRenderHandler GetRenderHandler()
        {
            return this._renderHandler;
        }

        private void _TrySetHost(CefBrowserHost host)
        {
            this._host = host;
            if (this._pendingUrl != null)
            {
                this._host.GetBrowser().GetMainFrame().LoadUrl(this._pendingUrl);
                this._pendingUrl = null;
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
                // TODO: How do we cancel?
                callback.Dispose();
            }
        }

        private class LifeSpanHandler : CefLifeSpanHandler
        {
            private readonly StationeersCefClient client;
            public LifeSpanHandler(StationeersCefClient client)
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
                client = null;
                return true;
            }
        }

        internal class RequestHandler : CefRequestHandler
        {
            private readonly ResourceRequestHandler _resourceRequestHandler;

            public RequestHandler(StationeersCefClient client)
            {
                this._resourceRequestHandler = new(client);
            }

            protected override CefResourceRequestHandler GetResourceRequestHandler(CefBrowser browser, CefFrame frame, CefRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
            {
                return this._resourceRequestHandler;
            }
        }

        internal class ResourceRequestHandler : CefResourceRequestHandler
        {
            private readonly StationeersCefClient _client;
            private readonly CookieAccessFilter _cookieAccessFilter = new();

            public ResourceRequestHandler(StationeersCefClient client)
            {
                this._client = client;
            }

            protected override CefCookieAccessFilter GetCookieAccessFilter(CefBrowser browser, CefFrame frame, CefRequest request)
            {
                return this._cookieAccessFilter;
            }

            protected override void OnProtocolExecution(CefBrowser browser, CefFrame frame, CefRequest request, ref bool allowOSExecution)
            {
                // Definitely do not let websites trigger OS level behaviors.
                allowOSExecution = false;
            }

            protected override CefReturnValue OnBeforeResourceLoad(CefBrowser browser, CefFrame frame, CefRequest request, CefRequestCallback callback)
            {
                if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
                {
                    return CefReturnValue.Cancel;
                }

                if (!IsUriAllowed(uri))
                {
                    Logging.LogError($"Blocked request to \"{uri}\" as it was not in the allow list.");
                    return CefReturnValue.Cancel;
                }

                return CefReturnValue.Continue;
            }

            private bool IsUriAllowed(Uri uri)
            {
                foreach (var allowUri in this._client._allowedUrls)
                {
                    if (uri.Scheme != allowUri.Scheme || uri.Host != allowUri.Host || uri.Port != allowUri.Port)
                    {
                        continue;
                    }

                    if (IsPathMatch(uri.AbsolutePath, allowUri.AbsolutePath))
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool IsPathMatch(string path, string allowPath)
            {
                var pathSegments = path.Trim('/').Split('/');
                var allowPathSegments = allowPath.Trim('/').Split('/');

                for (int i = 0; i < allowPathSegments.Length; i++)
                {
                    if (i >= pathSegments.Length)
                    {
                        return false;
                    }

                    if (allowPathSegments[i] == "**")
                    {
                        return true; // ** matches everything beyond this point
                    }

                    if (allowPathSegments[i] != "*" && allowPathSegments[i] != pathSegments[i])
                    {
                        return false;
                    }
                }

                return pathSegments.Length == allowPathSegments.Length;
            }
        }

        internal class CookieAccessFilter : CefCookieAccessFilter
        {
            protected override bool CanSaveCookie(CefBrowser browser, CefFrame frame, CefRequest request, CefResponse response, CefCookie cookie)
            {
                // Defaulting to overly strict until I can consider security.
                return false;
            }

            protected override bool CanSendCookie(CefBrowser browser, CefFrame frame, CefRequest request, CefCookie cookie)
            {
                // Defaulting to overly strict until I can consider security.
                return false;
            }
        }

        internal class RenderHandler : CefRenderHandler
        {
            private readonly AccessibilityHandler _accessibilityHandler = new();

            private readonly StationeersCefClient client;

            private readonly int _windowWidth;
            private readonly int _windowHeight;

            public RenderHandler(int windowWidth, int windowHeight, StationeersCefClient client)
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
                    lock (client._pixelLock)
                    {
                        if (browser != null)
                            Marshal.Copy(buffer, this.client._pixelBuffer, 0, this.client._pixelBuffer.Length);
                    }

                    client._pixelBufferInvalidated = true;
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
