a = 1
b = 3
c = 4
d = 51


def checkCase(val):
    if val == 1:
        print("equal 1")
        return
    elif val <= 3:
        print("equals 3")
    else:
        print("greater than 4")
        if val >50:
            print("huge")
        else:
            print("still big")

checkCase(a)
checkCase(b)
checkCase(c)
checkCase(d)