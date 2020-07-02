
import { TO_BasicPriceCurve } from './TO_BasicPriceCurve';
import { TO_ContangoPriceCurve } from './TO_ContangoPriceCurve';
import { TO_BasisPriceCurve } from './TO_BasisPriceCurve';
import { TO_ConstantPriceCurve } from './TO_ConstantPriceCurve';

export interface TO_PriceCurve  {
	 basicPriceCurve: TO_BasicPriceCurve;
	 contangoPriceCurve: TO_ContangoPriceCurve;
	 basisPriceCurve: TO_BasisPriceCurve;
	 constantPriceCurve: TO_ConstantPriceCurve;
}


