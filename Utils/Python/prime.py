def prime(n):
    flag = True
    for j in range(2,n+1):
        if (n % j ==0 and j!=n):
            flag = False
            return flag
        else:
            if n ==j:
                return flag
hexlist = ""
count = 0
num = 2
while count < 1000:
    if (prime(num)): 
            count += 1
            hexlist += "{0:#0{1}x}, ".format(num,6)
    num += 1
print(hexlist)