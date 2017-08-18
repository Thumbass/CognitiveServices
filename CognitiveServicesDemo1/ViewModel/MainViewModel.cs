﻿using CognitiveServicesDemo1.Interface;
using CognitiveServicesDemo1.Model;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CognitiveServicesDemo1.ViewModel
{
    class MainViewModel : ObservableObject
    {
        private TextToSpeak _textToSpeak;
        private string _filePath;
        private IFaceServiceClient _faceServiceClient;

        private BitmapImage _imageSource;

        public BitmapImage ImageSource
        {
            get { return _imageSource; }
            set
            {
                _imageSource = value;
                RaisePropertyChangedEvent("ImageSource");
            }
        }
        private string _statusText;

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;
                RaisePropertyChangedEvent("StatusText");
            }
        }
        public ICommand BrowseButtonCommand { get; private set; }
        public ICommand DetectFaceCommand { get; private set; }

        public MainViewModel()
        {
            StatusText = "Status: Waiting for image...";
            _faceServiceClient = new FaceServiceClient("cdd29de6f80542869e57d226520bdb96", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
            BrowseButtonCommand = new DelegateCommand(Browse);
            DetectFaceCommand = new DelegateCommand(DetectFace, CanDetectFace);

            _textToSpeak = new TextToSpeak();
            _textToSpeak.OnAudioAvailable += _textToSpeak_OnAudioAvailable;
            _textToSpeak.OnError += _textToSpeak_OnError;

            if (_textToSpeak.GenerateAuthenticationToken("CognitiveServicesDemo1", "3036c868000a48e2a991c0dadb9f2dd6"))
                _textToSpeak.GenerateHeaders();
        }
        private void _textToSpeak_OnError(object sender, AudioErrorEventArgs e)
        {
            StatusText = $"Status: Audio service failed -  {e.ErrorMessage}";
        }
        private void _textToSpeak_OnAudioAvailable(object sender, AudioEventArgs e)
        {
            SoundPlayer player = new SoundPlayer(e.EventData);
            player.Play();
            e.EventData.Dispose();
        }
        private void Browse(object obj)
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog();
            openDialog.Filter = "JPEG Image(*.jpg)|*.jpg";
            bool? result = openDialog.ShowDialog();
            if (!(bool)result) return;

            _filePath = openDialog.FileName;
            Uri fileUri = new Uri(_filePath);
            BitmapImage image = new BitmapImage(fileUri);
            image.CacheOption = BitmapCacheOption.None;
            image.UriSource = fileUri;
            ImageSource = image;
            StatusText = "Status: Image loaded...";
        }
        private bool CanDetectFace(object obj)
        {
            return !string.IsNullOrEmpty(ImageSource?.UriSource.ToString());
        }
        private async void DetectFace(object obj)
        {
            FaceRectangle[] faceRects = await UploadAndDetectFacesAsync();
            string textToSpeak = "No Faces detected";
            if (faceRects.Length == 1)
                textToSpeak = "1 face detected";
            else if (faceRects.Length > 1)
                textToSpeak = $"{faceRects.Length} faces detected";

            Debug.WriteLine(textToSpeak);
            await _textToSpeak.SpeakAsync(textToSpeak, CancellationToken.None);
        }
        private async Task<FaceRectangle[]> UploadAndDetectFacesAsync()
        {
            StatusText = "Status: Detecting faces...";
            
            try
            {
                using (Stream imageFileStream = File.OpenRead(_filePath))
                {
                    Face[] faces = await _faceServiceClient.DetectAsync(imageFileStream, true, true, new List<FaceAttributeType>() { FaceAttributeType.Age });
                    List<double> ages = faces.Select(face => face.FaceAttributes.Age).ToList();
                    FaceRectangle[] faceRects = faces.Select(face => face.FaceRectangle).ToArray();
                    StatusText = "Status: Finished detecting faces...";
                    foreach (var age in ages)
                    {
                        Console.WriteLine(age);
                    }
                    return faceRects;
                }
                
            }
            catch (Exception ex)
            {
                StatusText = $"Status: Failed to detect faces - {ex.Message}";

                return new FaceRectangle[0];
            }


        }
    }
}
