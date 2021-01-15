﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodeNameRestApi.Services;
using Microsoft.AspNetCore.Mvc;
using WordsDatabaseAPI.DatabaseModels.CollectionModels;
using WordsDatabaseAPI.DatabaseModels.ResultModels;

namespace CodeNameRestApi.Controllers
{
    [Route("api/[controller]/english")]
    [ApiController]
    public class CardController : ControllerBase
    {
        private readonly CardService _cardService;

        public CardController(CardService cardService)
        {
            _cardService = cardService;
        }

        [HttpGet("/{word:maxlength(15)}", Name = "GetCard")]
        public ActionResult<string> GetCardIfExists(string word)
        {
            var handler = _cardService._mongoHandler;
            Task<CardDocument> cardTask = handler.FindCardAsync(word);
            cardTask.Wait();

            CardDocument card = cardTask.Result;
            if (card == null)
            {
                return NotFound();
            }

            return card.Word;
        }

        [HttpGet("/count/", Name = "GetCount")]
        public ActionResult<long> GetCardsCount()
        {
            Task<long> countTask = _cardService._mongoHandler.GetDocumentsCountAsync();
            countTask.Wait();
            return countTask.Result;
        }

        [HttpGet("/random/{numberOfRandomNumbers:int}")]
        public ActionResult<CardDocument[]> GetRandomCards(int numberOfRandomNumbers)
        {
            if (numberOfRandomNumbers <= 0)
                return BadRequest();

            if (GetCardsCount().Value < numberOfRandomNumbers)
                return BadRequest();
            
            Task<RandomActionResult> randomTask = _cardService._mongoHandler.FindMultipleRandomCardsAsync((uint)numberOfRandomNumbers);
            randomTask.Wait();

            CardDocument[] randomCards = randomTask.Result.Result;
            if (randomCards.Length != numberOfRandomNumbers)
                return NotFound();

            return randomCards;
        }

        [HttpPost("")]
        public ActionResult<string> PostCardToDatabase(string word)
        {
            var handler = (WordsDatabaseAPI.DatabaseModels.MongoHandler)_cardService._mongoHandler;
            CardDocument card = new CardDocument(word);

            if (card == null)
                return NotFound(InsertActionResult.BAD_VALUE);

            InsertActionResult actionResult = handler.InsertCard(card);
            if (actionResult != InsertActionResult.OK)
                return BadRequest(actionResult.ToString());

            return CreatedAtRoute("GetCard", new { word = card.Word }, card.Word);
        }

        [HttpPut("{word}/{newWord}")]
        public IActionResult UpdateCardInDatabase(string word, string newWord)
        {
            UpdateActionResult actionResult = _cardService._mongoHandler.UpdateWord(word, newWord);
            if (actionResult != UpdateActionResult.OK)
                return BadRequest(actionResult.ToString());

            return NoContent();
        }
    }
}
