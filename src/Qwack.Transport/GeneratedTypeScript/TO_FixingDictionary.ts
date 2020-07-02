
import { FixingDictionaryType } from './FixingDictionaryType';

export interface TO_FixingDictionary  {
	 name: string;
	 assetId: string;
	 fxPair: string;
	 fixingDictionaryType: FixingDictionaryType;
	 fixings: { [key: string]: number; };
}


