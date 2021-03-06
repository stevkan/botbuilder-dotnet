// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Bot.Schema.Teams
{
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Represents the attachment in a message.
    /// </summary>
    public partial class MessageActionsPayloadAttachment
    {
        /// <summary>
        /// Initializes a new instance of the MessageActionsPayloadAttachment
        /// class.
        /// </summary>
        public MessageActionsPayloadAttachment()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the MessageActionsPayloadAttachment
        /// class.
        /// </summary>
        /// <param name="id">The id of the attachment.</param>
        /// <param name="contentType">The type of the attachment.</param>
        /// <param name="contentUrl">The url of the attachment, in case of a
        /// external link.</param>
        /// <param name="content">The content of the attachment, in case of a
        /// code snippet, email, or file.</param>
        /// <param name="name">The plaintext display name of the
        /// attachment.</param>
        /// <param name="thumbnailUrl">The url of a thumbnail image that might
        /// be embedded in the attachment, in case of a card.</param>
        public MessageActionsPayloadAttachment(string id = default(string), string contentType = default(string), string contentUrl = default(string), object content = default(object), string name = default(string), string thumbnailUrl = default(string))
        {
            Id = id;
            ContentType = contentType;
            ContentUrl = contentUrl;
            Content = content;
            Name = name;
            ThumbnailUrl = thumbnailUrl;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets the id of the attachment.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the attachment.
        /// </summary>
        [JsonProperty(PropertyName = "contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the url of the attachment, in case of a external link.
        /// </summary>
        [JsonProperty(PropertyName = "contentUrl")]
        public string ContentUrl { get; set; }

        /// <summary>
        /// Gets or sets the content of the attachment, in case of a code
        /// snippet, email, or file.
        /// </summary>
        [JsonProperty(PropertyName = "content")]
        public object Content { get; set; }

        /// <summary>
        /// Gets or sets the plaintext display name of the attachment.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the url of a thumbnail image that might be embedded in
        /// the attachment, in case of a card.
        /// </summary>
        [JsonProperty(PropertyName = "thumbnailUrl")]
        public string ThumbnailUrl { get; set; }

    }
}
