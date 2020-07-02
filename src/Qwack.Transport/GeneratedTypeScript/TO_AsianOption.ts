
import { OptionType } from './OptionType';
import { TO_AsianSwap } from './TO_AsianSwap';

export interface TO_AsianOption  extends TO_AsianSwap {
	 callPut: OptionType;
}


