
import { FundingInstrumentType } from './FundingInstrumentType';
import { AssetInstrumentType } from './AssetInstrumentType';
import { TO_AsianSwap } from './TO_AsianSwap';
import { TO_AsianSwapStrip } from './TO_AsianSwapStrip';
import { TO_AsianOption } from './TO_AsianOption';
import { TO_Forward } from './TO_Forward';
import { TO_EuropeanOption } from './TO_EuropeanOption';

export interface TO_Instrument  {
	 fundingInstrumentType: FundingInstrumentType;
	 assetInstrumentType: AssetInstrumentType;
	 asianSwap: TO_AsianSwap;
	 asianSwapStrip: TO_AsianSwapStrip;
	 asianOption: TO_AsianOption;
	 forward: TO_Forward;
	 europeanOption: TO_EuropeanOption;
}


