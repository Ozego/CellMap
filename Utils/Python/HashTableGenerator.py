import random
ht = list(range(0, 256))
random.shuffle(ht)
s = ""
for x in range(len(ht)): 
    if(x%16==0):
        s +="/n"
    s += '{:{align}{width}},'.format(str(ht[x]), align='>', width = '4')
print(s)

import random
ht = list(range(0, 10))
random.shuffle(ht)
s = ""
for x in range(len(ht)): 
    s += '{:{align}{width}}'.format(str(ht[x]), align='>', width = '1')
print(s)

