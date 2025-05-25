def check_values(a,b,c):
    if a == 1 and b == 1 and c == 1:
        return 0
    return -1

apple =1
orange = 1
bannana = 2

print(check_values(apple, orange, bannana))
bannana = bannana - 1
print(check_values(apple, orange, bannana))