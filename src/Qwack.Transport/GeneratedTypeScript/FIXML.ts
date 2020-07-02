
import { FIXMLMktDataFull } from './FIXMLMktDataFull';

export interface FIXML  {
	 batch: FIXMLMktDataFull[];
}import { FIXMLMktDataFullInstrmt } from './FIXMLMktDataFullInstrmt';
import { FIXMLMktDataFullFull } from './FIXMLMktDataFullFull';
import { FIXMLMktDataFullUndly } from './FIXMLMktDataFullUndly';
import { FIXMLMktDataFullAttrb } from './FIXMLMktDataFullAttrb';

export interface FIXMLMktDataFull  {
	 instrmt: FIXMLMktDataFullInstrmt;
	 full: FIXMLMktDataFullFull[];
	 undly: FIXMLMktDataFullUndly;
	 instrmtExt: FIXMLMktDataFullAttrb[];
	 bizDt: Date;
}import { FIXMLMktDataFullInstrmtEvnt } from './FIXMLMktDataFullInstrmtEvnt';

export interface FIXMLMktDataFullInstrmt  {
	 evnt: FIXMLMktDataFullInstrmtEvnt[];
	 sym: string;
	 id: string;
	 cfi: string;
	 secTyp: string;
	 src: string;
	 mmy: string;
	 matDt: Date;
	 strkPx: number;
	 putCall: boolean;
	 exch: string;
	 fnlSettlCcy: string;
}
export interface FIXMLMktDataFullUndly  {
	 sym: string;
	 id: string;
	 cfi: string;
	 secTyp: string;
	 src: string;
	 mmy: string;
	 matDt: Date;
	 strkPx: number;
	 putCall: boolean;
	 exch: string;
	 fnlSettlCcy: string;
}
export interface FIXMLMktDataFullInstrmtEvnt  {
	 eventTyp: number;
	 dt: Date;
	 txt: number;
	 txtSpecified: boolean;
}
export interface FIXMLMktDataFullFull  {
	 typ: string;
	 px: number;
	 pxSpecified: boolean;
	 mkt: string;
	 discntFctr: number;
	 discntFctrSpecified: boolean;
	 openClsSettlFlag: number;
	 openClsSettlFlagSpecified: boolean;
	 sz: number;
	 szSpecified: boolean;
	 pxTyp: number;
	 pxTypSpecified: boolean;
}
export interface FIXMLMktDataFullAttrb  {
	 typ: number;
	 val: string;
}


