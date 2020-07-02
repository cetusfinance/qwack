
import { TO_Interpolator2d_Jagged } from './TO_Interpolator2d_Jagged';
import { TO_Interpolator2d_Square } from './TO_Interpolator2d_Square';

export interface TO_Interpolator2d  {
	 jagged: TO_Interpolator2d_Jagged;
	 square: TO_Interpolator2d_Square;
	 isJagged: boolean;
}


