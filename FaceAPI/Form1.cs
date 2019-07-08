using AForge.Video;
using AForge.Video.DirectShow;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/*
 * https://docs.microsoft.com/ru-ru/azure/cognitive-services/face/tutorials/faceapiincsharptutorial
 */
namespace FaceAPI
{
    public partial class Form1 : Form
    {
        private const string subscriptionKey = "18981a28639e40c492ad866ad93cb1d2";
        private const string faceEndpoint = "https://westeurope.api.cognitive.microsoft.com";

        private readonly IFaceClient faceClient = new FaceClient(
            new ApiKeyServiceClientCredentials(subscriptionKey),
            new System.Net.Http.DelegatingHandler[] { }
        );

        FilterInfoCollection videoDevices;
        VideoCaptureDevice finalFrame;

        int counter = 0;
        bool reconInProcess = false;
        int frameId = 0;

        public Form1()
        {
            InitializeComponent();
            InitWebCamSelector();

            if (Uri.IsWellFormedUriString(faceEndpoint, UriKind.Absolute))
            {
                faceClient.Endpoint = faceEndpoint;
            }
            else
            {
                Environment.Exit(0);
            }
        }

        private void InitWebCamSelector()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            comboBox1.Items.Clear();
            foreach (FilterInfo i in videoDevices) comboBox1.Items.Add(i.Name);
            finalFrame = new VideoCaptureDevice();
            comboBox1.SelectedIndex = 0;

            StartVideo();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            StartVideo();
        }

        private void StartVideo()
        {
            if (finalFrame.IsRunning == true)
            {
                finalFrame.Stop();
            }

            finalFrame = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
            finalFrame.NewFrame += new NewFrameEventHandler(FinalFrame_NewFrame);
            finalFrame.Start();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (counter++ % 120 == 0)
            {
                if (!reconInProcess)
                {
                    (eventArgs.Frame.Clone() as Bitmap).Save($"./test-{++frameId}.bmp");
                }
            }
            pictureBox1.Image = (Bitmap)eventArgs.Frame.Clone();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (finalFrame.IsRunning == true) finalFrame.Stop();
        }
        private async Task UploadAndDetectFaces(string fileName)
        {
            reconInProcess = true;

            IList<FaceAttributeType> faceAttributes =
                new FaceAttributeType[]
                {
                    FaceAttributeType.Gender,
                    FaceAttributeType.Smile,
                    FaceAttributeType.Emotion
                };

            try
            {
                IList<DetectedFace> faceList = null;
                using (Stream imageFileStream = File.OpenRead(fileName))
                {
                    faceList =
                        await faceClient.Face.DetectWithStreamAsync(
                            imageFileStream, true, false, faceAttributes,
                            "recognition_02", true
                        );
                }
                if (faceList != null)
                {
                    DrawInfo(faceList, fileName);
                }
            }
            catch (APIErrorException f)
            {
                MessageBox.Show(f.Message);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error");
            }

            reconInProcess = false;
        }

        private async void DrawInfo(IList<DetectedFace> faceList, string fileName)
        {
            var backgroundImage = Image.FromFile(fileName).Clone() as Image;

            double smile = 0;
            double happiness = 0;
            int male = 0;
            int female = 0;
            int genderless = 0;
            using (var graphics = Graphics.FromImage(backgroundImage))
            {
                listBox1.Items.Clear();
                listBox2.Items.Clear();
                foreach (var face in faceList)
                {
                    listBox1.Items.Add(face.FaceId);
                    PersonInfo person = await IdentifyRequest(face.FaceId.ToString());
                    System.Drawing.Pen blackPen = new System.Drawing.Pen(System.Drawing.Color.Green, 1);
                    if (person != null)
                    {
                        blackPen = new System.Drawing.Pen(System.Drawing.Color.Red, 2);
                        listBox2.Items.Add($"{person.name}   <{person.personId}>");
                    }

                    var x1 = face.FaceRectangle.Left;
                    var y1 = face.FaceRectangle.Top;
                    var x2 = x1 + face.FaceRectangle.Width;
                    var y2 = y1 + face.FaceRectangle.Height;

                    graphics.DrawLine(blackPen, x1, y1, x2, y1);
                    graphics.DrawLine(blackPen, x1, y1, x1, y2);
                    graphics.DrawLine(blackPen, x2, y1, x2, y2);
                    graphics.DrawLine(blackPen, x1, y2, x2, y2);

                    smile += face.FaceAttributes.Smile.Value;
                    happiness += face.FaceAttributes.Emotion.Happiness;
                    switch (face.FaceAttributes.Gender)
                    {
                        case Gender.Female:
                            female += 1;
                            break;
                        case Gender.Male:
                            male += 1;
                            break;
                        case Gender.Genderless:
                            genderless += 1;
                            break;
                    }
                }
            }
            smile = smile / faceList.Count;
            happiness = happiness / faceList.Count;

            pictureBox2.Image = backgroundImage;

            label1.Text = $"Faces: {faceList.Count}";
            label2.Text = $"Smile prob: {smile}";
            label3.Text = $"Happiness prob: {happiness}";

            label4.Text = $"Female: {female}";
            label5.Text = $"Male: {male}";
            label6.Text = $"Genderless: {genderless}";
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            UploadAndDetectFaces($"./test-{frameId}.bmp");
        }

        private async Task<PersonInfo> IdentifyRequest(string faceId)
        {
            PersonInfo person = null;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var uri = $"{faceEndpoint}/face/v1.0/identify?";
            HttpResponseMessage response;

            string data = "{\"personGroupId\": \"testpersongroup1\",\"faceIds\": [\"" + faceId + "\"], \"maxNumOfCandidatesReturned\": 1, \"confidenceThreshold\": 0.8}";
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                try
                {
                    response = await client.PostAsync(uri, content);
                    string respBody = await response.Content.ReadAsStringAsync();
                    
                    IdentifyResponce[] result = JsonConvert.DeserializeObject<IdentifyResponce[]>(respBody);

                    if (result.Length == 1 && result[0].faceId == faceId && result[0].candidates.Length == 1)
                    {
                        person = await GetPersonRequest(result[0].candidates[0].personId);
                    }
                }
                catch (Exception e)
                {
                    // MessageBox.Show(e.Message);
                }
            }

            return person;
        }

        private async Task<PersonInfo> GetPersonRequest(string personId)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = $"{faceEndpoint}/face/v1.0/persongroups/testpersongroup1/persons/{personId}?";

            var response = await client.GetAsync(uri);
            string respBody = await response.Content.ReadAsStringAsync();
            PersonInfo person = JsonConvert.DeserializeObject<PersonInfo>(respBody);

            return person;
        }

        bool processing = false;

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (processing)
            {
                UploadAndDetectFaces($"./test-{frameId}.bmp");
            }
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            processing = true;
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            processing = false;
        }
    }
}
