__global float * input_A;
__global float * output_W;
__global float G1;
__global int size_X;

__global float * fSums;

kernel void Init(global float* input_X, global float* output_Z, const float G, const int size, global float * fsums)
{
	input_A = input_X;
	output_W = output_Z;
	G1 = G;
	size_X = size;
	fSums = fsums;

	printf("kernel variable Init\n");
}

kernel void Attract()
{
	int i = get_global_id(0);
	int j = get_global_id(1);
	/*
	if (i == 0 && j == 0) {
		printf("%f\n", input_A[0]);
	}*/

	float distanceX = output_W[j * 2] - output_W[i * 2];
	float distanceY = output_W[j * 2 + 1] - output_W[i * 2 + 1];
	float x2_y2 = distanceX * distanceX + distanceY * distanceY;

	float dist = sqrt(x2_y2 * x2_y2 * x2_y2);
	float b = G1 * input_A[j * 3 + 2] / (dist + 0.000001f);

	fSums[(j * size_X + i) * 2] = distanceX * b;
	fSums[(j * size_X + i) * 2 + 1] = distanceY * b;
}

kernel void Move()
{
	int i = get_global_id(0);

	/*if (i == 0){
		printf("%f\n", input_A[0]);
	}*/
	
	for (int j = 0; j < size_X; j++) 
	{
		input_A[i * 3] += fSums[(j * size_X + i) * 2];
		input_A[i * 3 + 1] += fSums[(j * size_X + i) * 2 + 1];
	}

	float vx = input_A[i * 3];
	float vy = input_A[i * 3 + 1];

	output_W[i * 2] += vx;
	output_W[i * 2 + 1] += vy;
}

