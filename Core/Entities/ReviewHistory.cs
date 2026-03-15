using System;
using System.ComponentModel.DataAnnotations;

namespace VocabTrainer.Core.Entities
{
    /// <summary>
    /// Records the number of card reviews completed on a given UTC date.
    /// One row per day — upserted after each training session.
    /// </summary>
    public class ReviewHistory
    {
        [Key]
        public DateTime Date { get; set; }      // UTC date (time stripped)
        public int CardsReviewed { get; set; }  // total answers that day
        public int CorrectAnswers { get; set; }
    }
}
