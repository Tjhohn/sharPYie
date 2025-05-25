def rule110(a, b, c):
    if a == 1 and b == 1 and c == 1:
       return 0
    if a == 1 and b == 1 and c == 0:
       return 1
    if a == 1 and b == 0 and c == 1:
       return 1
    if a == 1 and b == 0 and c == 0:
       return 0
    if a == 0 and b == 1 and c == 1:
       return 1
    if a == 0 and b == 1 and c == 0:
       return 1
    if a == 0 and b == 0 and c == 1:
       return 1
    return 0

def compute_next(cells, i, n):
    if i >= n:
        return []
    if i == 0:
        left = 0
    else:
        left = cells[i - 1]
    center = cells[i]
    if i == n - 1:
        right = 0
    else:
        right = cells[i + 1]
    return [rule110(left, center, right)] + compute_next(cells, i + 1, n)

def run(cells, gen):
    if gen == 0:
        return
    print_line(cells, 0, len(cells))
    next_cells = compute_next(cells, 0, len(cells))
    run(next_cells, gen - 1)

def print_line(cells, i, n):
    if i >= n:
        print()
        return
    if cells[i] == 1:
        print('█', end='')
    else:
        print(' ', end='')
    print_line(cells, i + 1, n)

# Initial state: one '1' in the middle
init = [0]*20 + [1] + [0]*20
run(init, 20)
