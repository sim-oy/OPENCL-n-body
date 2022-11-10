__global float * input_A;
__global float * output_W;
__global float G1;
__global int size_X;

__global float * fSums;

inline void AtomicAdd(volatile __global float* source, const float operand) {
	union {
		unsigned int intVal;
		float floatVal;
	} newVal;
	union {
		unsigned int intVal;
		float floatVal;
	} prevVal;
	do {
		prevVal.floatVal = *source;
		newVal.floatVal = prevVal.floatVal + operand;
	} while (atomic_cmpxchg((volatile __global unsigned int*)source, prevVal.intVal, newVal.intVal) != prevVal.intVal);
}

inline void atomicAdd_g_f(volatile __global float* addr, float val)
{
	union {
		unsigned int u32;
		float f32;
	} next, expected, current;
	current.f32 = *addr;
	do {
		expected.f32 = current.f32;
		next.f32 = expected.f32 + val;
		current.u32 = atomic_cmpxchg((volatile __global unsigned int*)addr,
			expected.u32, next.u32);
	} while (current.u32 != expected.u32);
}

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
	/*
	if (i == 0) {
		printf("%f\n", input_A[0]);
	}*/

	for (int j = i + 1; j < size_X; j++)
	{
		float distanceX = output_W[j * 2] - output_W[i * 2];
		float distanceY = output_W[j * 2 + 1] - output_W[i * 2 + 1];
		float x2_y2 = distanceX * distanceX + distanceY * distanceY;

		float dist = sqrt(x2_y2 * x2_y2 * x2_y2);

		float b = G1 / (dist + 0.000001f);

		double Ai = particles[j].mass * b;
		double Aj = particles[i].mass * b;

		sumXi += distanceX * Ai;
		sumYi += distanceY * Ai;

		//sumXi += distanceX * b;
		//sumYi += distanceY * b;

		input_A[j * 3] += -distanceX * Aj;
		input_A[j * 3 + 1] += -distanceY * Aj;
	}
	
	input_A[i * 3] += sumXi;
	input_A[i * 3 + 1] += sumYi;

	//input_A[i * 3] += distanceX * b;
	//input_A[i * 3 + 1] += distanceY * b;
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