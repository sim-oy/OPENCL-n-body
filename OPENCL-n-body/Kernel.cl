

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

kernel void Attract(global float * input_X, const float G)
{

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

kernel void Move(/*global float* input_X*/) {

    int i = get_global_id(0);

    float vx = input_X[i * 5 + 2];
    float vy = input_X[i * 5 + 3];

    AtomicAdd(&input_X[i * 5], vx);
    AtomicAdd(&input_X[i * 5 + 1], vy);

}