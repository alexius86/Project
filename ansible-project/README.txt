***Basic setup before ansible-playbook call***

1. Copy "ansible-project" in user's home directory

2. It's required that Linux user has "sudo" privilege

2. Change username in roles/basic/vars/main.yml to specify current Linux user

3. Make sure that user's public key is registered in authorized_keys file: ~/.ssh/authorized_keys
   - if is not, execute the following commands:
     1. ssh-keygen ( on every prompt type "Enter")
     2. cat ~/.ssh/id_rsa.pub >> ~/.ssh/authorized_keys
     3. chmod og-wx ~/.ssh/authorized_keys

4. Execute ansible-playbook in "ansible-project" directory: 
   - ansible-playbook -K playbook.yml