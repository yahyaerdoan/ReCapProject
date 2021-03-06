﻿using Core.Entities.Abstract;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Concrete
{
    public class Card : IEntity
    {
        public int CardId { get; set; }
        public int CustomerId { get; set; }
        public string CardholderFirstNameLastName { get; set; }
        public string CreditCardNumber { get; set; }
        public string ValidThru { get; set; }
        public string CardValidationValue { get; set; }        
    }
}
