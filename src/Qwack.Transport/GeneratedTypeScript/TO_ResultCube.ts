
import { TO_ResultCubeRow } from './TO_ResultCubeRow';

export interface TO_ResultCube  {
	 rows: TO_ResultCubeRow[];
	 types: { [key: string]: string; };
	 fieldNames: string[];
}


