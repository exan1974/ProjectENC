using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Linq;

public class VoiceCommandController : MonoBehaviour
{
    [Header("Voice Recognition Settings")]
    [SerializeField] private float minConfidence = 0.7f;
    [SerializeField] private bool useKeywordRecognizer = true;

    [Header("Command Keywords")]
    [SerializeField] private string freezeCommand = "freeze";
    [SerializeField] private string unfreezeCommand = "unfreeze";
    [SerializeField] private string speedUpCommand = "speed up";
    [SerializeField] private string slowDownCommand = "slow down";
    [SerializeField] private string resetCommand = "reset";

    private KeywordRecognizer keywordRecognizer;
    private DictationRecognizer dictationRecognizer;
    private BufferedNeuronMimic neuronMimic;
    private Dictionary<string, System.Action> commands;

    void Start()
    {
        neuronMimic = GetComponent<BufferedNeuronMimic>();
        if (neuronMimic == null)
        {
            Debug.LogError("[VoiceCommandController] BufferedNeuronMimic component not found!");
            enabled = false;
            return;
        }

        InitializeCommands();
        InitializeSpeechRecognition();
    }

    private void InitializeCommands()
    {
        commands = new Dictionary<string, System.Action>
        {
            { freezeCommand, () => neuronMimic.ToggleFreeze() },
            { unfreezeCommand, () => neuronMimic.ToggleFreeze() },
            { speedUpCommand, () => neuronMimic.IncreaseSpeed() },
            { slowDownCommand, () => neuronMimic.DecreaseSpeed() },
            { resetCommand, () => neuronMimic.ClearBuffer() }
        };
    }

    private void InitializeSpeechRecognition()
    {
        if (useKeywordRecognizer)
        {
            // Use keyword recognition for better performance
            keywordRecognizer = new KeywordRecognizer(commands.Keys.ToArray(), ConfidenceLevel.Medium);
            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
            keywordRecognizer.Start();
        }
        else
        {
            // Use dictation for more flexible commands
            dictationRecognizer = new DictationRecognizer(ConfidenceLevel.Medium);
            dictationRecognizer.DictationResult += OnDictationResult;
            dictationRecognizer.Start();
        }
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        float confidence = GetConfidenceValue(args.confidence);
        if (confidence >= minConfidence && commands.ContainsKey(args.text))
        {
            Debug.Log($"[VoiceCommandController] Recognized command: {args.text} (confidence: {confidence})");
            commands[args.text].Invoke();
        }
    }

    private void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        float confidenceValue = GetConfidenceValue(confidence);
        if (confidenceValue >= minConfidence)
        {
            text = text.ToLower();
            foreach (var command in commands)
            {
                if (text.Contains(command.Key))
                {
                    Debug.Log($"[VoiceCommandController] Recognized command: {command.Key} in phrase: {text}");
                    command.Value.Invoke();
                    break;
                }
            }
        }
    }

    private float GetConfidenceValue(ConfidenceLevel level)
    {
        switch (level)
        {
            case ConfidenceLevel.High:
                return 0.9f;
            case ConfidenceLevel.Medium:
                return 0.7f;
            case ConfidenceLevel.Low:
                return 0.5f;
            case ConfidenceLevel.Rejected:
                return 0.0f;
            default:
                return 0.0f;
        }
    }

    void OnDestroy()
    {
        if (keywordRecognizer != null)
        {
            keywordRecognizer.OnPhraseRecognized -= OnPhraseRecognized;
            keywordRecognizer.Dispose();
        }

        if (dictationRecognizer != null)
        {
            dictationRecognizer.DictationResult -= OnDictationResult;
            dictationRecognizer.Dispose();
        }
    }

    // Public method to toggle between keyword and dictation recognition
    public void ToggleRecognitionMode()
    {
        useKeywordRecognizer = !useKeywordRecognizer;
        
        if (keywordRecognizer != null)
        {
            keywordRecognizer.Stop();
            keywordRecognizer.Dispose();
        }

        if (dictationRecognizer != null)
        {
            dictationRecognizer.Stop();
            dictationRecognizer.Dispose();
        }

        InitializeSpeechRecognition();
    }

    // Public method to update confidence threshold
    public void SetConfidenceThreshold(float threshold)
    {
        minConfidence = Mathf.Clamp01(threshold);
    }
} 