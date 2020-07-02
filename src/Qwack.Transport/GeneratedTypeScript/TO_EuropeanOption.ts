
import { OptionType } from './OptionType';
import { TO_Forward } from './TO_Forward';

export interface TO_EuropeanOption  extends TO_Forward {
	 callPut: OptionType;
}


