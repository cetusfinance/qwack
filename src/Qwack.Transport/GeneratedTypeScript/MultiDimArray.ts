

export interface MultiDimArray<T>  {
	 length0: number;
	 length1: number;
	 jaggedLengths: number[];
	 backingArray: T[];
}


