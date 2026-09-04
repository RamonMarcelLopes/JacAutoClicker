using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace JacaAutoClicker.Presentation;

public class MainForm : Form
{
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    private const string VirtualHost = "jacaclicker.app";

    private readonly WebViewBridge _bridge;
    private readonly WebView2 _webView;

    public MainForm(WebViewBridge bridge)
    {
        _bridge = bridge;
        _bridge.MinimizeRequested += () => WindowState = FormWindowState.Minimized;
        _bridge.CloseRequested += Close;
        _bridge.DragRequested += StartWindowDrag;

        Text = "JacAuto Clicker";
        ClientSize = new Size(470, 730);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(11, 18, 25);

        string icoPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");
        if (File.Exists(icoPath)) Icon = new Icon(icoPath);

        Region = new Region(RoundRect(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 22));

        _webView = new WebView2 { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.Transparent };
        Controls.Add(_webView);

        Load += MainForm_Load;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        await _webView.EnsureCoreWebView2Async();
        _webView.DefaultBackgroundColor = Color.Transparent;

        string webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(VirtualHost, webRoot, CoreWebView2HostResourceAccessKind.Allow);
        _webView.CoreWebView2.WebMessageReceived += (_, args) => _bridge.HandleMessage(args.WebMessageAsJson);

        _bridge.AttachWebView(_webView);
        _webView.CoreWebView2.Navigate($"https://{VirtualHost}/index.html");
    }

    private void StartWindowDrag()
    {
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundRect(Rectangle r, int rad)
    {
        int d = rad * 2;
        var p = new System.Drawing.Drawing2D.GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _bridge.Dispose();
        base.OnFormClosed(e);
    }
}
