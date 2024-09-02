﻿using System.Runtime.CompilerServices;

namespace GeonBit.UI.Entities.TextValidators
{

	/// <summary>
	/// Make sure input is numeric and optionally validate min / max values.
	/// </summary>
	[System.Serializable]
	public class TextValidatorNumbersOnly : ITextValidator
	{
		/// <summary>
		/// Static ctor.
		/// </summary>
		static TextValidatorNumbersOnly()
		{
			Entity.MakeSerializable(typeof(TextValidatorNumbersOnly));
		}

		/// <summary>
		/// Do we allow decimal point?
		/// </summary>
		public bool AllowDecimalPoint;

		/// <summary>
		/// Optional min value.
		/// </summary>
		public double? Min;

		/// <summary>
		/// Optional max value.
		/// </summary>
		public double? Max;

		/// <summary>
		/// Create the number validator.
		/// </summary>
		/// <param name="allowDecimal">If true, will allow decimal point in input.</param>
		/// <param name="min">If provided, will force min value.</param>
		/// <param name="max">If provided, will force max value.</param>
		public TextValidatorNumbersOnly(bool allowDecimal, double? min = null, double? max = null)
		{
			AllowDecimalPoint = allowDecimal;
			Min = min;
			Max = max;
		}

		/// <summary>
		/// Create number validator with default params.
		/// </summary>
		public TextValidatorNumbersOnly() : this(false)
		{
		}

		/// <summary>
		/// Return true if text input is a valid number.
		/// </summary>
		/// <param name="text">New text input value.</param>
		/// <param name="oldText">Previous text input value.</param>
		/// <returns>If TextInput value is legal.</returns>
		public override bool ValidateText(ref string text, string oldText)
		{
			// if string empty return true
			if(text.Length == 0)
			{
				return true;
			}

			// will contain value as number
			double num;

			// try to parse as double
			if(AllowDecimalPoint)
			{
				if(!double.TryParse(text, out num))
				{
					return false;
				}
			}
			// try to parse as int
			else
			{
				int temp;
				if(!int.TryParse(text, out temp))
				{
					return false;
				}
				num = temp;
			}

			// validate range
			if(Min != null && num < (double)Min) { text = Min.ToString(); }
			if(Max != null && num > (double)Max) { text = Max.ToString(); }

			// valid number input
			return true;
		}
	}

	
}