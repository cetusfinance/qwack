
import { TO_FxMatrix } from './TO_FxMatrix';
import { TO_IrCurve } from './TO_IrCurve';
import { TO_VolSurface } from './TO_VolSurface';

export interface TO_FundingModel  {
	 curves: { [key: string]: TO_IrCurve; };
	 volSurfaces: { [key: string]: TO_VolSurface; };
	 buildDate: Date;
	 fxMatrix: TO_FxMatrix;
}


