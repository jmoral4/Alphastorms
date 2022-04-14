namespace Alphastorms.Downloader.Win
{
    public partial class Form1 : Form
    {
        DownloaderApp app;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DownloaderConfig config = DownloaderConfig.FromIni("config.ini");

            this.lblInitialMessage.Text = config.WelcomeMessage;
            this.lblProgress.Text = config.DefaultStateMessage;
            this.tbServerMessage.Text = config.DefaultMessage;
            this.pbDownloader.ImageLocation = config.DefaultImage;


            app = new DownloaderApp(config);
            
        }
    }
}