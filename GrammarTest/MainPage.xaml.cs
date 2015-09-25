using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.ApplicationModel;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Nito.AsyncEx;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace GrammarTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SpeechRecognizer _recognizer;
        private bool _audioRunning;
        private bool _notAudioCommand;


        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {

            await InitializeSpeechRecognizer();

            //var displayRequest = new Windows.System.Display.DisplayRequest();
            //displayRequest.RequestActive();
        }


        private async Task WriteBlecOmmand(string command)
        {
            try
            {
                _commandExecuting = true;

                BluetoothLEDevice oygenBluetoothLeDevice = null;
                GattCharacteristic writeCharacteristics = null;

                oygenBluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(0x000780067cf1);


                var srv =
                    oygenBluetoothLeDevice.GattServices.FirstOrDefault(
                        gs => gs.Uuid.ToString().Equals("2ac94b65-c8f4-48a4-804a-c03bc6960b80"));

                Debug.WriteLine(oygenBluetoothLeDevice.Name);

                writeCharacteristics =
                    srv
                        .GetAllCharacteristics()
                        .FirstOrDefault(ch => ch.Uuid.ToString().Equals("50e03f22-b496-4a73-9e85-335482ed4b12"));

                var writer = new DataWriter();

                writer.WriteString(command + "\n");

                await writeCharacteristics.WriteValueAsync(writer.DetachBuffer());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Bluetooth Problem.");
                Debug.WriteLine(ex.Message);
            }
        }


      

        // Initialize Speech Recognizer and start async recognition
        private async Task InitializeSpeechRecognizer()
        {
            // Initialize recognizer
            _recognizer = new SpeechRecognizer();


            // Set event handlers
            _recognizer.StateChanged += Recognizer_StateChanged;
            _recognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;


            // Load Grammer file constraint
            string fileName = String.Format("Grammar\\Grammar.xml");
            StorageFile grammarContentFile = await Package.Current.InstalledLocation.GetFileAsync(fileName);

            SpeechRecognitionGrammarFileConstraint grammarConstraint =
                new SpeechRecognitionGrammarFileConstraint(grammarContentFile);


            // Add to grammer constraint
            _recognizer.Constraints.Add(grammarConstraint);

            // Compile grammer
            SpeechRecognitionCompilationResult compilationResult = await _recognizer.CompileConstraintsAsync();

            Debug.WriteLine("Status: " + compilationResult.Status.ToString());

            // If successful, display the recognition result.
            if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
            {


                await _recognizer.ContinuousRecognitionSession.StartAsync();
            }
            else
            {

            }
        }


    

        private bool _commandExecuting = false;

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
           
            try
            {
              
                //Debug.WriteLine(Enum.GetName(typeof(SpeechRecognitionConfidence), args.Result.Confidence));

                if (!_commandExecuting)
                {

                    Debug.WriteLine(args.Result.Text);

                    _commandExecuting = true;
                    if (args.Result.Text.Contains("call") && args.Result.Text.Contains("MUMMY"))
                    {
                        _notAudioCommand = true;
                        SpeechSynthesizer synt = new SpeechSynthesizer();


                        SpeechSynthesisStream syntStream =
                            await synt.SynthesizeTextToStreamAsync("Calling your mum. Just a moment.");

                        await
                           Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () => { mediaElement.SetSource(syntStream, syntStream.ContentType); });

                        await SendCommandToServer("CallMummy");

                       
                    }


                    if (args.Result.Text.Contains("light") &&
                        args.Result.Text.Contains("ON") && args.Result.Text.Contains("turn"))
                    {
                       
                        SpeechSynthesizer synt = new SpeechSynthesizer();
                        SpeechSynthesisStream syntStream =
                            await synt.SynthesizeTextToStreamAsync("Success! Lights are active!");
                        await
                            Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                  async () =>
                                {

                                    mediaElement.SetSource(syntStream, syntStream.ContentType);
                                    await WriteBlecOmmand("l1on\n");
                                    await WriteBlecOmmand("l2on\n");
                                });

                        _notAudioCommand = true;

                    }



                    if (args.Result.Text.Contains("light") &&
                        args.Result.Text.Contains("turn") && args.Result.Text.Contains("OFF"))
                    {
                       
                        SpeechSynthesizer synt = new SpeechSynthesizer();
                        SpeechSynthesisStream syntStream =
                            await synt.SynthesizeTextToStreamAsync("Success! No lights active!");
                        await
                            Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                 async () =>
                                {
                                    mediaElement.SetSource(syntStream, syntStream.ContentType);
                                    await WriteBlecOmmand("l1off\n");
                                    await WriteBlecOmmand("l2off\n");
                                });

                        _notAudioCommand = true;
                    }

                    if (args.Result.Text.Contains("call") && args.Result.Text.Contains("DADDY"))
                    {
                        _notAudioCommand = true;
                        SpeechSynthesizer synt = new SpeechSynthesizer();
                        SpeechSynthesisStream syntStream =
                            await synt.SynthesizeTextToStreamAsync("Calling your dad. Just a moment. ");

                        await
                           Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                               () => { mediaElement.SetSource(syntStream, syntStream.ContentType); });

                        await SendCommandToServer("CallDaddy");

                       
                    }

                    //if (args.Result.Text.Contains("emergency") && args.Result.Text.Contains("call"))
                    //{
                    //    _notAudioCommand = true;
                    //    SpeechSynthesizer synt = new SpeechSynthesizer();
                    //    SpeechSynthesisStream syntStream =
                    //        await
                    //            synt.SynthesizeTextToStreamAsync(
                    //                "Calling nine one one. It will take only a moment sweety -  if you can, try to get help and take your phone with you. If not stay calm and wait for the operator to respond.");
                    //    await
                    //        Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    //            () => { mediaElement.SetSource(syntStream, syntStream.ContentType); });
                    //}

                    if (!_audioRunning)
                    {
                        _notAudioCommand = true;
                        if (args.Result.Text.Contains("one") && args.Result.Text.Contains("soundtrack") &&
                                       args.Result.Text.Contains("play"))
                        {
                            await SendCommandToServer("PlaySong1");

                            _audioRunning = true;

                            SpeechSynthesizer synt = new SpeechSynthesizer();
                            SpeechSynthesisStream syntStream =
                                await synt.SynthesizeTextToStreamAsync("Playing soundtrack one");
                            await
                                Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                    () => { mediaElement.SetSource(syntStream, syntStream.ContentType); });
                        }

                        if (args.Result.Text.Contains("two") && args.Result.Text.Contains("soundtrack") &&
                            args.Result.Text.Contains("play"))
                        {
                            await SendCommandToServer("PlaySong2");

                            _audioRunning = true;

                            SpeechSynthesizer synt = new SpeechSynthesizer();
                            SpeechSynthesisStream syntStream =
                                await synt.SynthesizeTextToStreamAsync("Playing soundtrack two");
                            await
                                Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                    () => { mediaElement.SetSource(syntStream, syntStream.ContentType); });
                        }

                        if (args.Result.Text.Contains("three") && args.Result.Text.Contains("soundtrack") &&
                            args.Result.Text.Contains("play"))
                        {
                            _audioRunning = true;

                            await SendCommandToServer("PlaySong3");

                            SpeechSynthesizer synt = new SpeechSynthesizer();
                            SpeechSynthesisStream syntStream =
                                await synt.SynthesizeTextToStreamAsync("Playing soundtrack three");
                            await
                                Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                    () => { mediaElement.SetSource(syntStream, syntStream.ContentType); });
                        }

                        if (args.Result.Text.Contains("four") && args.Result.Text.Contains("soundtrack") &&
                            args.Result.Text.Contains("play"))
                        {
                            _audioRunning = true;

                            await SendCommandToServer("PlaySong4");

                            SpeechSynthesizer synt = new SpeechSynthesizer();
                            SpeechSynthesisStream syntStream =
                                await synt.SynthesizeTextToStreamAsync("Playing soundtrack four");
                            await
                                Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                    () => { mediaElement.SetSource(syntStream, syntStream.ContentType); });
                        }
                        
                    }
                    


                    if (args.Result.Text.Contains("iron") && args.Result.Text.Contains("man") &&
                       (args.Result.Text.Contains("green") || args.Result.Text.Contains("red") || args.Result.Text.Contains("blue") || args.Result.Text.Contains("off")))
                    {

                        _notAudioCommand = true;

                        SpeechSynthesizer synt = new SpeechSynthesizer();
                        SpeechSynthesisStream syntStream =
                            await synt.SynthesizeTextToStreamAsync("Success! Color changed!");
                        await
                            Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                                async () =>
                                {
                                    mediaElement.SetSource(syntStream, syntStream.ContentType);
                                    if (args.Result.Text.Contains("red"))
                                    {
                                        await WriteBlecOmmand("boff\n");
                                        await WriteBlecOmmand("bred\n");
                                    }

                                    if (args.Result.Text.Contains("green"))
                                    {
                                        await WriteBlecOmmand("boff\n");
                                        await WriteBlecOmmand("bgreen\n");
                                    }

                                    if (args.Result.Text.Contains("blue"))
                                    {
                                        await WriteBlecOmmand("boff\n");
                                        await WriteBlecOmmand("bblue\n");
                                    }
                                    if (args.Result.Text.Contains("off"))
                                    {
                                        await WriteBlecOmmand("boff\n");

                                    }
                                });


                    }



                    if (args.Result.Text.Contains("clock") &&
                        (args.Result.Text.Contains("on") || args.Result.Text.Contains("off")))
                    {
                        _notAudioCommand = true;
                        SpeechSynthesizer synt = new SpeechSynthesizer();
                        SpeechSynthesisStream syntStream =
                            await synt.SynthesizeTextToStreamAsync("Success! Time information panel status changed.");

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                             {
                                 mediaElement.SetSource(syntStream, syntStream.ContentType);
                             });

                        if (args.Result.Text.Contains("off"))
                        {
                            await SendCommandToServer("DimmClock");
                        }

                        if (args.Result.Text.Contains("on"))
                        {
                            await SendCommandToServer("UndimmClock");
                        }

                    }

                    if (args.Result.Text.Contains("audio") &&
                      (args.Result.Text.Contains("pause") || args.Result.Text.Contains("stop") || args.Result.Text.Contains("continue")))
                    {
                        _notAudioCommand = false;

                        if (_audioRunning)
                        {
                            string commandText = default(string);

                            if (args.Result.Text.Contains("pause"))
                            {
                                commandText = "Media waiting state!";
                                await SendCommandToServer("PauseSong");
                                _audioRunning = true;
                            }

                            if (args.Result.Text.Contains("continue"))
                            {
                                commandText = "Media resuming!";
                                await SendCommandToServer("ResumeSong");
                                _audioRunning = true;
                            }

                            if (args.Result.Text.Contains("stop"))
                            {
                                commandText = "Media off!";
                                await SendCommandToServer("StopSong");
                                _audioRunning = false;
                                _notAudioCommand = true;
                            }



                            SpeechSynthesizer synt = new SpeechSynthesizer();
                            SpeechSynthesisStream syntStream =
                                await synt.SynthesizeTextToStreamAsync(commandText);

                            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                mediaElement.SetSource(syntStream, syntStream.ContentType);
                            });
                        }



                    }


                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

          
        }

        private void Recognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            if (args.State == SpeechRecognizerState.Idle || args.State == SpeechRecognizerState.SoundEnded)
            {
                _commandExecuting = false;
            }
            else
            {
                _commandExecuting = true;
            } 
            

        }


        private async Task SendCommandToServer(string command)
        {
            HttpClient cl = new HttpClient();

            var data = command;

            var response = await cl.PostAsync("[REPLACE WITH THE URL OF YOUR PI RUNNING THE NITE-LITE SERVER]",
                       new StringContent("\"" + command + "\"",
                           Encoding.UTF8, "application/json"));

           




        }

    }
}