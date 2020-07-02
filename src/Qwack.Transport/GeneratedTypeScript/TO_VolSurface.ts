
import { TO_ConstantVolSurface } from './TO_ConstantVolSurface';
import { TO_GridVolSurface } from './TO_GridVolSurface';
import { TO_RiskyFlySurface } from './TO_RiskyFlySurface';

export interface TO_VolSurface  {
	 constantVolSurface: TO_ConstantVolSurface;
	 gridVolSurface: TO_GridVolSurface;
	 riskyFlySurface: TO_RiskyFlySurface;
}


