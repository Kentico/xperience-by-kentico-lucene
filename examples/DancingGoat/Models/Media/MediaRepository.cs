using System;
using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;
using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of media files stored in the content hub.
    /// </summary>
    public class MediaRepository
    {
        private readonly IPageRetriever pageRetriever;


        /// <summary>
        /// Initializes a new instance of the <see cref="MediaRepository"/> class.
        /// </summary>
        /// <param name="pageRetriever">Retriever for pages based on given parameters.</param>
        public MediaRepository(IPageRetriever pageRetriever)
        {
            this.pageRetriever = pageRetriever;
        }


        /// <summary>
        /// Returns media file based on given node identifier.
        /// </summary>
        /// <param name="nodeId">Identifier of the node.</param>
        public Media GetMediaFile(int nodeId)
        {
            return pageRetriever.Retrieve<Media>(
                query => query.WhereEquals("NodeID", nodeId),
                cache => cache
                    .Key($"{nameof(MediaRepository)}|{nameof(GetMediaFile)}|{nodeId}"))
                    .FirstOrDefault();
        }
    }
}
