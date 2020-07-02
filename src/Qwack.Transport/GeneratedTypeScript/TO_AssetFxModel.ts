
import { TO_FundingModel } from './TO_FundingModel';
import { TO_CorrelationMatrix } from './TO_CorrelationMatrix';
import { TO_Portfolio } from './TO_Portfolio';
import { TO_VolSurface } from './TO_VolSurface';
import { TO_PriceCurve } from './TO_PriceCurve';
import { TO_FixingDictionary } from './TO_FixingDictionary';

export interface TO_AssetFxModel  {
	 assetVols: { [key: string]: TO_VolSurface; };
	 assetCurves: { [key: string]: TO_PriceCurve; };
	 fixings: { [key: string]: TO_FixingDictionary; };
	 buildDate: Date;
	 fundingModel: TO_FundingModel;
	 correlationMatrix: TO_CorrelationMatrix;
	 portfolio: TO_Portfolio;
}


