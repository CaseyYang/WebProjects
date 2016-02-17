from xml.etree import ElementTree

contactFilePath = "D:\Photo\sh\Contact_QQPhoneManager(2016-02-09).xml"
smsRecordFilePath = "D:\Photo\sh\Sms_QQPhoneManager(2016-02-09).xml"
contacts = {}
invertedContacts = {}
smsRecords = {}

contactFile = ElementTree.parse(contactFilePath)
#获取所有contact节点
for contact in contactFile.findall("Contact"):
	name = contact.find("Name")
	if name == None:
		continue
	else:
		name = name.text
		if " " in name:
			name = name[::-1]
		name = "".join(name.split())
		phoneList = contact.find("PhoneList")
		for phoneNumber in phoneList.getchildren():
			if phoneNumber.tag == "Phone":
				rawNumber = phoneNumber.text
				if len(rawNumber) > 11:
					rawNumber = rawNumber[-11:]
			if rawNumber not in invertedContacts.keys():
				invertedContacts[rawNumber] = {}
			invertedContacts[rawNumber].setdefault(name)
			if name not in contacts.keys():
				contacts[name] = {}
			contacts[name].setdefault(rawNumber)

#获取短信记录
smsFile = ElementTree.parse(smsRecordFilePath)
for sms in smsFile.findall("SMS"):
	type = sms.find("Type").text
	number = sms.find("Address").text
	date = sms.find("Date").text
	content = sms.find("Body").text.replace("\n"," ")
	name = ""
	sender = ""
	if len(number) > 11:
		number = number[-11:]
	if number not in invertedContacts.keys():
		name = number
	elif len(invertedContacts[number]) > 1:
           print(number)
	else: 
		name = next(iter(invertedContacts[number].keys()))
	if type == "1":
		sender = name
	else:
		sender = "苏昊"
	if name not in smsRecords.keys():
		smsRecords[name]=[]
	smsRecords[name].append({"date":date,"sender":sender,"content":content})


#输出通讯录到文件
#resultContact = open("contact.txt","w")
#for contact in contacts:
#	resultContact.write(contact + '\t')
#	for number in contacts[contact]:
#		resultContact.write(number + '\t')
#	resultContact.write('\n')
#resultContact.close()

#倒排通讯录输出到文件
#resultInvertedContact = open("invertedContact.txt","w")
#for number in invertedContacts:
#	resultInvertedContact.write(number + '\t')
#	for name in invertedContacts[number]:
#		resultInvertedContact.write(name + '\t')
#	resultInvertedContact.write('\n')
#resultInvertedContact.close()

#输出短信记录到文件
#windows中txt文件默认使用GBK编码；
#使用utf-8编码使得原始记录中的各种符号可以输出
resultSMS = open("sms.txt","w",encoding='utf-8')
for name in smsRecords:
	resultSMS.write("\n"+name+":\n")
	for record in smsRecords[name]:
		try:
			resultSMS.write(record["date"]+"\t")
			resultSMS.write(record["sender"]+"\t")		
			resultSMS.write(record["content"])
		except BaseException:
			print("Error")
		finally:
			resultSMS.write("\n")
resultSMS.close()
