__global float * input_A;
__global float G1;

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

kernel void Init(global float* input_X, const float G)
{
    input_A = input_X;
    G1 = G;
}

kernel void Attract()
{
    int i = get_global_id(0);
    int j = get_global_id(1);

    float distanceX = input_A[j * 5] - input_A[i * 5];
    float distanceY = input_A[j * 5 + 1] - input_A[i * 5 + 1];
    float x2_y2 = distanceX * distanceX + distanceY * distanceY;

    float dist = sqrt(x2_y2 * x2_y2 * x2_y2);

    float b = G1 * input_A[j * 5 + 4] / (dist + 0.000001f);
       
    AtomicAdd(&input_A[i * 5 + 2], distanceX * b);
    AtomicAdd(&input_A[i * 5 + 3], distanceY * b);
}

kernel void Move(/*global float* input_X*/)
{

    int i = get_global_id(0);

    if (i == 0){
        printf("%f\n", input_A[0]);
    }

    float vx = input_A[i * 5 + 2];
    float vy = input_A[i * 5 + 3];

    AtomicAdd(&input_A[i * 5], vx);
    AtomicAdd(&input_A[i * 5 + 1], vy);
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