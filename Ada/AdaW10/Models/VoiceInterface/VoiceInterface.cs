﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;
using AdaSDK;
using AdaW10.Models.VoiceInterface.SpeechToText;
using AdaW10.Models.VoiceInterface.TextToSpeech;
using AdaW10.Helper;

namespace AdaW10.Models.VoiceInterface
{
    public class VoiceInterface
    {
        #region Speaking section

        public async Task SayHelloAsync(PersonDto[] persons)
        {
            if (persons == null) return;

            var listPersons = persons
                .Select(p => p)
                .Where(p => !string.IsNullOrEmpty(p.FirstName) && p.NbPasses <= 1)
                .ToArray();

            if (listPersons.Any())
            {
                await TtsService.SayAsync(SpeechDictionnary.GetHelloSentence(listPersons));
            }
        }

        public async Task SayHelloAsync(PersonDto person)
        {
            if (person == null) return;

            if (person.FirstName == null) await TtsService.SayAsync("Bonjour, il semblerait que je ne te connaisse pas.");
            else await TtsService.SayAsync($"Bonjour {person.FirstName}");
        }

        public async Task SayNotAvailableService()
        {
            await TtsService.SayAsync("Ce service n'est pas encore disponible !");
        }

        public async Task SayEventsAvailable()
        {
            await TtsService.SayAsync(SpeechDictionnary.GetEventsAvailableSentence());
        }

        public async Task SayDescriptionOfSomeone(PersonDto person)
        {
            if (person == null) return;

            var genderSentence = person.Gender == GenderValues.Male ? "un homme" : "une femme";

            await TtsService.SayAsync($"Il semblerait que tu soies {genderSentence} de {person.Age} ans");
        }

        public async Task SayGoodBye()
        {
            await TtsService.SayAsync("Passe une bonne journée !");
        }

        #endregion

        #region Listening section

        private SttService _continuousRecognitionSession;

        public async Task PrepareListening()
        {
            if (_continuousRecognitionSession != null){
                await StopListening(); 
            }

            _continuousRecognitionSession = new SttService();
        }

        public async Task StopListening()
        {
            if (_continuousRecognitionSession != null)
            {
                if (await _continuousRecognitionSession.CancelContinuousRecognitionAsync())
                {
                    _continuousRecognitionSession.Dispose();
                    _continuousRecognitionSession = null;
                }
            }
        }

        public async Task ListeningHelloAda()
        {
            await PrepareListening();

            await _continuousRecognitionSession.AddConstraintAsync(ConstraintsDictionnary.ConstraintForHelloAda);
            await _continuousRecognitionSession.StartContinuousRecognitionAsync();
        }

        public async Task ListeningCancellation()
        {
            await PrepareListening(); 
            await _continuousRecognitionSession.AddConstraintAsync(ConstraintsDictionnary.ConstraintForAbortWords);
            await _continuousRecognitionSession.StartContinuousRecognitionAsync();
        }

        public async Task ListeningWhatToDo()
        {
            await PrepareListening(); 

            await TtsService.SayAsync(SpeechDictionnary.GetAskWhatToDoSentence());

            await _continuousRecognitionSession.AddConstraintAsync(ConstraintsDictionnary.ConstraintForEvents, false);
            await _continuousRecognitionSession.AddConstraintAsync(ConstraintsDictionnary.ConstraintForAbortWords, false);
            await _continuousRecognitionSession.AddConstraintAsync(ConstraintsDictionnary.ConstraintForSandwichs, false);
            await _continuousRecognitionSession.AddConstraintAsync(ConstraintsDictionnary.ConstraintForCallingSomeone, false);
            await _continuousRecognitionSession.AddConstraintAsync(ConstraintsDictionnary.ConstraintForToDescribeSomeOne, false);
            await _continuousRecognitionSession.AddConstraintAsync(ConstraintsDictionnary.ConstraintForReservation);

            await _continuousRecognitionSession.StartContinuousRecognitionAsync();
        }

        #endregion

        #region Instant recognitions section 

        public async Task<string> Listen()
        {
            using (var sttService = new SttService())
            {
                await sttService.AddConstraintAsync(ConstraintsDictionnary.GetConstraintForSpeak());

                var result = await sttService.RecognizeAsync();

                return result.Text;
            }
        }

        public async Task<string> AskReason()
        {
            using (var sttService = new SttService())
            {
                await TtsService.SayAsync(SpeechDictionnary.GetReasonSentence());

                await sttService.AddConstraintAsync(ConstraintsDictionnary.ConstraintForCertification, false);
                await sttService.AddConstraintAsync(ConstraintsDictionnary.ConstraintForMeeting, false);
                await sttService.AddConstraintAsync(ConstraintsDictionnary.ConstraintForSandwichs, false);
                await sttService.AddConstraintAsync(ConstraintsDictionnary.ConstraintForOtherWords);

                var result = await sttService.RecognizeAsync();

                if (result.Confidence != SpeechRecognitionConfidence.Rejected)
                {
                    var firedConstraint = (SpeechRecognitionListConstraint)result.Constraint;
                    return firedConstraint.Commands.First();
                }

                return "Autre";
            }
        }

        public async Task<string> AskIdentified()
        {
            using (var sttService = new SttService())
            {
                await TtsService.SayAsync("Veux tu t'identifier ?");

                await sttService.AddConstraintAsync(ConstraintsDictionnary.ConstraintForYes, false);
                await sttService.AddConstraintAsync(ConstraintsDictionnary.ConstraintForNo);

                var result = await sttService.RecognizeAsync();

                if (result.Confidence != SpeechRecognitionConfidence.Rejected)
                {
                    var firedConstraint = (SpeechRecognitionListConstraint)result.Constraint;
                    return firedConstraint.Commands.First();
                }

                return "non";
            }

        }

        public async Task<string> AskNameAsync()
        {
            using (var sttService = new SttService())
            {
                bool repeat;
                string name;

                do
                {
                    // --> 1 : Asking the name
                    await TtsService.SayAsync(SpeechDictionnary.GetAskNameSentence());

                    await sttService.AddConstraintAsync(ConstraintsDictionnary.GetConstraintForName());

                    var result = await RecognitionWithFallBack(sttService);
                    name = result.Text;

                    // Clean up stt service to clean constraints of prévious recognitions
                    await sttService.CleanConstraintsAsync();

                    // --> 3 : Asking confirmation for the name
                    await sttService.AddConstraintAsync(ConstraintsDictionnary.ConstraintForYes, false);
                    await sttService.AddConstraintAsync(ConstraintsDictionnary.ConstraintForNo, false);
                    await sttService.AddConstraintAsync(ConstraintsDictionnary.ConstraintForAbortWords);

                    await TtsService.SayAsync(SpeechDictionnary.GetYesOrNoSentence(result.Text));

                    result = await RecognitionWithFallBack(sttService);

                    var firedConstraint = (SpeechRecognitionListConstraint)result.Constraint;

                    switch (firedConstraint.Tag)
                    {
                        case "constraint_yes":
                            repeat = false;
                            break;

                        case "constraint_abord_words":
                            name = null;
                            repeat = false;
                            break;

                        default:
                            repeat = true;
                            break;
                    }

                    await sttService.CleanConstraintsAsync();

                } while (repeat);

                return name;
            }
        }

        private static async Task<SpeechRecognitionResult> RecognitionWithFallBack(SttService sttService)
        {
            SpeechRecognitionResult result;

            do
            {
                result = await sttService.RecognizeAsync();

                if (result.Confidence == SpeechRecognitionConfidence.Rejected){
                    await TtsService.SayAsync(SpeechDictionnary.GetNotUnderstoodSentence());
                }
                else{
                    break;
                }

            } while (true);

            return result;
        }

        #endregion
    }
}
