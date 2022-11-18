__global float ** input_A;
__global float ** output_W;
__global float G1;
__global int size_X;

kernel void Init(global float** input_X, global float** output_Z, const float G, const int size)
{
	input_A = input_X;
	output_W = output_Z;
	G1 = G;
	size_X = size;

	printf("kernel variable Init\n");
}

kernel void Attract()//Attract 1
{
	int i = get_global_id(0);

	float xi = output_W[i * 2];
	float yi = output_W[i * 2 + 1];
	float sumX = 0, sumY = 0;
	for (int j = 0; j < size_X; j++)
	{
		float distanceX = output_W[j * 2] - xi;
		float distanceY = output_W[j * 2 + 1] - yi;

		float x2_y2 = distanceX * distanceX + distanceY * distanceY;
		float dist = sqrt(x2_y2 * x2_y2 * x2_y2);

		float b = G1 * input_A[j * 3 + 2] / (dist + 0.000001f);
		
		sumX += distanceX * b;
		sumY += distanceY * b;
	}
	input_A[i * 3] += sumX;
	input_A[i * 3 + 1] += sumY;
}

kernel void Move()
{
	int i = get_global_id(0);

	float vx = input_A[i * 3];
	float vy = input_A[i * 3 + 1];

	//AtomicAdd(&output_W[i * 2], vx);
	//AtomicAdd(&output_W[i * 2 + 1], vy);

	//atomicAdd_g_f(&output_W[i * 2], vx);
	//atomicAdd_g_f(&output_W[i * 2 + 1], vy);

	output_W[i * 2] += vx;
	output_W[i * 2 + 1] += vy;
}


/*
kernel void Attract(global float* input_X, const float G)
{
	input_A = input_X;

	int i = get_global_id(0);
	int j = get_global_id(1);

	float distanceX = input_X[j * 5] - input_X[i * 5];
	float distanceY = input_X[j * 5 + 1] - input_X[i * 5 + 1];
	float x2_y2 = distanceX * distanceX + distanceY * distanceY;

	float dist = sqrt(x2_y2 * x2_y2 * x2_y2);

	float b = G * input_X[j * 5 + 4] / (dist + 0.000001f);

	AtomicAdd(&input_X[i * 5 + 2], distanceX * b);
	AtomicAdd(&input_X[i * 5 + 3], distanceY * b);
}
*/