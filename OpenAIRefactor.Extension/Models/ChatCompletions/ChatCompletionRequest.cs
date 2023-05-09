﻿using Newtonsoft.Json;
using OpenAIRefactor.Models.Common;
using OpenAIRefactor.Settings;
using SharpToken;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAIRefactor.Models.ChatCompletions
{

    /// <summary>Represents a chat completion request message</summary>
    public class ChatCompletionRequest : RequestBase
    {

        /// <summary>Initializes a new instance of the <see cref="ChatCompletionRequest" /> class.</summary>
        /// <param name="chatMessage">The chat message.</param>
        /// <exception cref="System.ArgumentNullException">chatMessage</exception>
        public ChatCompletionRequest(ChatMessage chatMessage)
        {
            if (chatMessage == null) throw new ArgumentNullException(nameof(chatMessage));

            Messages.Add(chatMessage);
        }

        /// <summary>Initializes a new instance of the <see cref="ChatCompletionRequest" /> class.</summary>
        /// <param name="chatMessage">The chat message.</param>
        /// <param name="model">The model.</param>
        /// <exception cref="System.ArgumentNullException">model</exception>
        public ChatCompletionRequest(ChatMessage chatMessage, string model) : this(chatMessage)
        {
            if (string.IsNullOrWhiteSpace(model)) throw new ArgumentNullException(nameof(model));

            Model = model;
        }

        /// <summary>Initializes a new instance of the <see cref="ChatCompletionRequest" /> class.</summary>
        /// <param name="chatMessages">The chat messages.</param>
        /// <exception cref="System.ArgumentNullException">chatMessages</exception>
        public ChatCompletionRequest(IEnumerable<ChatMessage> chatMessages)
        {
            if (chatMessages == null) throw new ArgumentNullException(nameof(chatMessages));

            Messages.AddRange(chatMessages);
        }

        /// <summary>Initializes a new instance of the <see cref="ChatCompletionRequest" /> class.</summary>
        /// <param name="chatMessages">The chat messages.</param>
        /// <param name="model">The model.</param>
        /// <exception cref="System.ArgumentNullException">model</exception>
        public ChatCompletionRequest(IEnumerable<ChatMessage> chatMessages, string model) : this(chatMessages)
        {
            if (string.IsNullOrWhiteSpace(model)) throw new ArgumentNullException(nameof(model));

            Model = model;
        }

        /// <summary>
        /// ID of the model to use.<br/>
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-model" />
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; } = OpenAIDefaultOptions.DefaultChatCompletionModel;

        /// <summary>
        /// The messages to generate chat completions for, in the chat format.
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-messages" />
        /// </summary>
        /// <value>The messages.</value>
        [JsonProperty("messages")]
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

        /// <summary>
        /// What <a href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-temperature">sampling temperature</a> to use. Higher values means the model will take more risks. <br/>
        /// Try 0.9 for more creative applications, and 0 (argmax sampling) for ones with a well-defined answer. <br/>
        /// We generally recommend altering this or top_p but not both. <br/>
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-temperature" />
        /// </summary>
        [JsonProperty("temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. <br/>
        /// So 0.1 means only the tokens comprising the top 10% probability mass are considered. <br/>
        /// We generally recommend altering this or temperature but not both. <br/>
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-top_p" />
        /// </summary>
        [JsonProperty("top_p")]
        public double? TopP { get; set; }

        /// <summary>
        /// How many completions to generate for each prompt. <br/>
        /// Note: Because this parameter generates many completions, it can quickly consume your token quota. <br/>
        /// Use carefully and ensure that you have reasonable settings for max_tokens and stop. <br/>
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-n" />
        /// </summary>
        [JsonProperty("n")]
        public int? NumberOfChoicesPerMessage { get; set; }

        /// <summary>
        /// Whether to stream back partial progress.  <br/>
        /// If set, tokens will be sent as data-only <a href="https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events#Event_stream_format">server-sent</a> events as they become available, with the stream terminated by a data: [DONE] message.  <br/>
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-stream" />
        /// </summary>
        [JsonProperty("stream")]
        public bool Stream { get; set; }

        /// <summary>
        /// Up to 4 sequences where the API will stop generating further tokens. <br/>
        /// The returned text will not contain the stop sequence. <br/>
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-stop" />
        /// </summary>
        [JsonProperty("stop")]
        public string[] StopSequences { get; set; }

        /// <summary>
        /// The stop sequence where the API will stop generating further tokens. The returned text will not contain the stop sequence.
        /// For convenience, if you are only requesting a single stop sequence, set it here
        /// </summary>
        [JsonIgnore]
        public string StopSequence
        {
            get => StopSequences?.FirstOrDefault();
            set
            {
                if (value == null)
                {
                    StopSequences = Array.Empty<string>();
                }
                else
                {
                    if (StopSequences.Length == 1)
                    {
                        StopSequences[0] = value;
                    }
                    else
                    {
                        StopSequences = new[] { value };
                    }
                }
            }
        }

        /// <summary>
        /// The maximum number of <a href="https://beta.openai.com/tokenizer">tokens</a> to generate in the completion. <br/> 
        ///  The token count of your prompt plus max_tokens cannot exceed the model's context length. Most models have a context length of 2048 tokens (except for the newest models, which support 4096). <br/>
        ///  <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-max_tokens" />
        /// </summary>
        /// 
        int? maxTokens;
        [JsonProperty("max_tokens")]
        public int? MaxTokens
        {
            get
            {
                var gptEncoding = GptEncoding.GetEncodingForModel("gpt-3.5-turbo");
                if (maxTokens == null && Messages.Count > 0)
                {
                    maxTokens = 2048 - gptEncoding.Encode(Messages[0].Content).Count;
                }
                return maxTokens;
            }
            set
            {
                maxTokens = value;
            }
        }

        /// <summary>
        /// Number between -2.0 and 2.0. Positive values penalize new tokens based on whether they
        /// appear in the text so far, increasing the model's likelihood to talk about new topics.
        /// <a href="https://platform.openai.com/docs/api-reference/parameter-details">See more information about frequency and presence penalties.</a> <br/>
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-presence_penalty" />
        /// </summary>
        [JsonProperty("presence_penalty")]
        public double? PresencePenalty { get; set; }

        /// <summary>
        /// Number between -2.0 and 2.0. <br/>
        /// Positive values penalize new tokens based on their existing frequency in the text so far, decreasing the model's likelihood to repeat the same line verbatim. <br/>
        /// <a href="https://platform.openai.com/docs/api-reference/parameter-details">See more information about frequency and presence penalties.</a> <br/>
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-frequency_penalty" />
        /// </summary>
        [JsonProperty("frequency_penalty")]
        public double? FrequencyPenalty { get; set; }

        /// <summary>
        /// Modify the likelihood of specified tokens appearing in the completion. <br/>
        /// Accepts a json object that maps tokens(specified by their token ID in the GPT tokenizer) to an associated bias value from -100 to 100. <br/>
        /// You can use this <a href="https://beta.openai.com/tokenizer?view=bpe"> tokenizer tool </a> (which works for both GPT-2 and GPT-3) to convert text to token IDs. <br/>
        /// Mathematically, the bias is added to the logits generated by the model prior to sampling. <br/>
        /// The exact effect will vary per model, but values between -1 and 1 should decrease or increase likelihood of selection; values like -100 or 100 should result in a ban or exclusive selection of the relevant token. <br/>
        /// As an example, you can pass  <br/>
        /// to prevent the  <![CDATA[<|endoftext|>]]>  token from being generated. <br/>
        /// <see href="https://platform.openai.com/docs/api-reference/chat/create#chat/create-logit_bias" />
        /// </summary>
        [JsonProperty("logit_bias")]
        public Dictionary<string, double> LogitBias { get; set; }

        /// <summary>
        /// A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse. <a href="https://platform.openai.com/docs/guides/safety-best-practices/end-user-ids">Learn more</a>.
        /// </summary>
        [JsonProperty("user")]
        public string User { get; set; }

    }

}
