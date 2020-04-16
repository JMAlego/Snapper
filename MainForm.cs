using System;
using System.Threading;
using System.Windows.Forms;

namespace Snapper
{
    public partial class MainForm : Form
    {
        private enum KeyModifier : int
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        private readonly int HOTKEY_MODIFIER = KeyModifier.WinKey.GetHashCode() | KeyModifier.Alt.GetHashCode();

        private readonly int TOP_HOTKEY_KEY = Keys.Up.GetHashCode();
        private const int TOP_HOTKEY_ID = 0;
        private readonly int BOTTOM_HOTKEY_KEY = Keys.Down.GetHashCode();
        private const int BOTTOM_HOTKEY_ID = 1;

        private Mutex singletonMutex = new Mutex(false, "polyomino.xyz Snapper SingletonApplicationWarning");
        private bool aquiredSingletonMutex = false;

        /// <summary>
        /// Register hotkeys with the system.
        /// </summary>
        private void RegisterKeys()
        {
            Native.RegisterHotKey(this.Handle, TOP_HOTKEY_ID, HOTKEY_MODIFIER, TOP_HOTKEY_KEY);
            Native.RegisterHotKey(this.Handle, BOTTOM_HOTKEY_ID, HOTKEY_MODIFIER, BOTTOM_HOTKEY_KEY);
        }

        /// <summary>
        /// Unregister hotkeys with the system.
        /// </summary>
        private void UnRegisterKeys()
        {
            Native.UnregisterHotKey(this.Handle, TOP_HOTKEY_ID);
            Native.UnregisterHotKey(this.Handle, BOTTOM_HOTKEY_ID);
        }

        /// <summary>
        /// Main form, registers hotkeys on instantiation.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            SingletonCheck();
            HandleFirstLaunch();
            RegisterKeys();
        }

        /// <summary>
        /// Ensure that only one copy of the application is running at a time.
        /// </summary>
        private void SingletonCheck()
        {
            if (singletonMutex.WaitOne(TimeSpan.Zero))
            {
                aquiredSingletonMutex = true;
            }
            else
            {
                MessageBox.Show("Only one instance of this application should really be running at a time.", "This application is already running!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Handle displaying an info box on first launch (and only on first launch).
        /// </summary>
        private void HandleFirstLaunch()
        {
            // If settings require an upgrade, as detailed by the "Upgrade Required" settings:
            if (Properties.Settings.Default.UpgradeRequired)
            {
                // Do upgrade, and then save.
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            // If this is the first launch of the application:
            if (Properties.Settings.Default.FirstLaunch)
            {
                // Display a helpful message box to tell people it's in their tray.
                MessageBox.Show("Snapper! is running in the background.\r\n" +
                                "You can open it from your system tray.\r\n" +
                                "(You won't see this message again.)", "Snapper! is running in the background.",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                Properties.Settings.Default.FirstLaunch = false;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Get window border width on left or right.
        /// </summary>
        /// <param name="window">HWND handle of the window to get the border of.</param>
        /// <returns>The width of the window's left or right border in pixels.</returns>
        private int GetWindowXBorder(IntPtr window)
        {
            Native.RECT clientRect = new Native.RECT();
            Native.RECT windowRect = new Native.RECT();

            Native.GetClientRect(window, out clientRect);
            Native.GetWindowRect(window, out windowRect);

            return ((windowRect.Right - windowRect.Left) - clientRect.Right) / 2;
        }

        /// <summary>
        /// Get window border height of the top border.
        /// </summary>
        /// <param name="window">HWND handle of the window to get the border of.</param>
        /// <returns>The height of the window's top border in pixels.</returns>
        private int GetWindowYBorder(IntPtr window)
        {
            Native.RECT clientRect = new Native.RECT();
            Native.RECT windowRect = new Native.RECT();

            Native.GetClientRect(window, out clientRect);
            Native.GetWindowRect(window, out windowRect);

            return ((windowRect.Bottom - windowRect.Top) - clientRect.Bottom) / 2;
        }

        /// <summary>
        /// Get the size the specified window should be to take up half the working area of the screen it is on's height.
        /// </summary>
        /// <param name="window">HWND handle of the window to get the half screen height of.</param>
        /// <returns>The calculated height in pixels.</returns>
        private int GetHalfScreenWindowHeight(IntPtr window)
        {
            Screen windowScreen = Screen.FromHandle(window);
            return windowScreen.WorkingArea.Height / 2 + GetWindowYBorder(window) * 2;
        }

        /// <summary>
        /// Set the position and size of a window.
        /// </summary>
        /// <param name="window">HWND handle of the window to set the position of.</param>
        /// <param name="x">The new x position of the top left hand corner.</param>
        /// <param name="y">The new y position of the top left hand corner.</param>
        /// <param name="width">The new width of the window.</param>
        /// <param name="height">The new height of the window.</param>
        /// <param name="async">Whether the resize should be done asynchronously or not.</param>
        private void SetWindowPosition(IntPtr window, int x, int y, int width, int height, bool async)
        {
            Native.SetWindowPos(window, IntPtr.Zero, x, y, width, height, Native.SWP_NOCOPYBITS | (async ? Native.SWP_ASYNCWINDOWPOS : 0x0));
        }

        /// <summary>
        /// Set the position and size of a window.
        /// </summary>
        /// <param name="window">HWND handle of the window to set the position of.</param>
        /// <param name="x">The new x position of the top left hand corner.</param>
        /// <param name="y">The new y position of the top left hand corner.</param>
        /// <param name="width">The new width of the window.</param>
        /// <param name="height">The new height of the window.</param>
        private void SetWindowPosition(IntPtr window, int x, int y, int width, int height)
        {
            SetWindowPosition(window, x, y, width, height, false);
        }

        /// <summary>
        /// Snap the currently focussed window to the top of the screen.
        /// </summary>
        private void SnapTop()
        {
            // Get current window and the screen it's on.
            IntPtr focussedWindow = Native.GetForegroundWindow();
            Screen windowScreen = Screen.FromHandle(focussedWindow);

            // Get window x border.
            int windowBorderX = GetWindowXBorder(focussedWindow);

            // Calculate new window size.
            int newWindowWidth = windowScreen.WorkingArea.Width + windowBorderX * 2;
            int newWindowHeight = GetHalfScreenWindowHeight(focussedWindow);

            // Calculate new window position.
            int newWindowTopLeftCornerX = windowScreen.WorkingArea.Location.X - windowBorderX;
            int newWindowTopLeftCornerY = windowScreen.WorkingArea.Location.Y;

            // Move window.
            SetWindowPosition(focussedWindow, newWindowTopLeftCornerX, newWindowTopLeftCornerY, newWindowWidth, newWindowHeight);

            Native.POINT actualBottomPosition = new Native.POINT(newWindowTopLeftCornerX, newWindowTopLeftCornerY + newWindowHeight);
            Native.ScreenToClient(focussedWindow, ref actualBottomPosition);

            // Some applications have more complex borders, this second check catches that and resizes the window again.
            if (actualBottomPosition.Y != newWindowHeight)
            {
                newWindowHeight = actualBottomPosition.Y;

                SetWindowPosition(focussedWindow, newWindowTopLeftCornerX, newWindowTopLeftCornerY, newWindowWidth, newWindowHeight, true);
            }
        }

        /// <summary>
        /// Snap the currently focussed window to the bottom of the screen.
        /// </summary>
        private void SnapBottom()
        {
            // Get current window and the screen it's on.
            IntPtr focussedWindow = Native.GetForegroundWindow();
            Screen windowScreen = Screen.FromHandle(focussedWindow);

            // Get window x border.
            int windowBorderX = GetWindowXBorder(focussedWindow);

            // Calculate new window size.
            int newWindowWidth = windowScreen.WorkingArea.Width + windowBorderX * 2;
            int newWindowHeight = GetHalfScreenWindowHeight(focussedWindow);

            // Calculate new window position.
            int newWindowTopLeftCornerX = windowScreen.WorkingArea.Location.X - windowBorderX;
            int newWindowTopLeftCornerY = windowScreen.WorkingArea.Bottom - newWindowHeight + GetWindowYBorder(focussedWindow) * 2;

            // Move window.
            SetWindowPosition(focussedWindow, newWindowTopLeftCornerX, newWindowTopLeftCornerY, newWindowWidth, newWindowHeight);

            Native.POINT actualBottomPosition = new Native.POINT(newWindowTopLeftCornerX, newWindowTopLeftCornerY + newWindowHeight);
            Native.ScreenToClient(focussedWindow, ref actualBottomPosition);

            // Some applications have more complex borders, this second check catches that and resizes the window again.
            if (actualBottomPosition.Y != newWindowHeight)
            {
                newWindowHeight = actualBottomPosition.Y;

                SetWindowPosition(focussedWindow, newWindowTopLeftCornerX, newWindowTopLeftCornerY, newWindowWidth, newWindowHeight, true);
            }
        }

        /// <summary>
        /// Override the base WndProc adding the hotkey handler.
        /// </summary>
        /// <param name="m">The message to be handled.</param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // Handle the hotkey message
            if (m.Msg == Native.WM_HOTKEY)
            {
                // If we recognise the hotkey id, handle it.
                switch (m.WParam.ToInt32())
                {
                    case TOP_HOTKEY_ID:
                        SnapTop();
                        break;
                    case BOTTOM_HOTKEY_ID:
                        SnapBottom();
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Show the form after being minimised to the tray.
        /// </summary>
        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// Form resize handler.
        /// </summary>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            // If form was just minimised.
            if (WindowState == FormWindowState.Minimized)
            {
                // Hide the form as well, it can be show using the tray icon.
                Hide();
            }
        }

        /// <summary>
        /// Form closing handler.
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Clean up hotkey registration.
            UnRegisterKeys();
            if (aquiredSingletonMutex)
                singletonMutex.ReleaseMutex();
        }

        /// <summary>
        /// Tray icon double click handler.
        /// </summary>
        private void TrayIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowFromTray();
        }

        /// <summary>
        /// Form closed handler.
        /// </summary>
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // This needs to be done manually as we started with this form hidden.
            Application.Exit();
        }

        /// <summary>
        /// Tray tool strip show handler.
        /// </summary>
        private void ShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFromTray();
        }

        /// <summary>
        /// Tray tool strip quit handler.
        /// </summary>
        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
