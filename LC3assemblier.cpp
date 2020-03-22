#include<iostream>
#include<cstdio>
#include<map>
#include<vector>
#include<string>
#include<string.h>
#include<fstream>
#include<iomanip>

using namespace std;

map <string,int> instructionMap;


struct unit{
    string labelName;
    string instructCode;
    bool haveLabel=0;
    int numOffset;
    unit(){
        haveLabel=0;
    }
};


enum TokenType{
    FakeInstruct,
    Instruction,
    Register,
    ImmediateNumber,
    Label,
    Comma,
    String,
    EndOfFile,
    Other
};

struct token{
    int tokenType;
    int numValue;
    string strValue;
    token(){}
};

char fGetChar(FILE* fp,int &line){
    static char c=' ';
    if(c=='\n')line++;
    if(fscanf(fp, "%c", &c)==1){
        return c;
    }
    else
        return EOF;
}

//void getToken(token &curToken,FILE *fp){}


int convertImm(string input,int base){
    try{
        int retValue=stoi(input,nullptr,base);
        if(retValue>0x7fff&&retValue<=0xffff)
            return retValue-0x10000;
        return retValue;
    }
    catch (const std::exception& ex){
        throw "immediate number out of range";
    }
}

void getToken(token &curToken,FILE* fp,int &line){
    static char nextChar=' ';
    //escape comment
    while(nextChar==';'){
        while(nextChar!='\n'&&nextChar!=EOF)
            nextChar=fGetChar(fp,line);
        if(nextChar==EOF){
            curToken.tokenType=TokenType::EndOfFile;
            return;
        }
        nextChar=fGetChar(fp,line);//eat \n
    }
    //escape white space
    while(isspace(nextChar)){
        while(isspace(nextChar))
            nextChar=fGetChar(fp,line);
        //escape comment
        while(nextChar==';'){
            while(nextChar!='\n'&&nextChar!=EOF)
                nextChar=fGetChar(fp,line);
            if(nextChar==EOF){
                curToken.tokenType=TokenType::EndOfFile;
                return;
            }
            nextChar=fGetChar(fp,line);//eat \n
        }
    }
    //fake instruction
    if(nextChar=='.'){
        string inputString="";
        nextChar=fGetChar(fp,line);//eat .
        while(isalpha(nextChar)){
            inputString+=nextChar;
            nextChar=fGetChar(fp,line);
        }
        // convert string to upper case
        for(int i=0;i<inputString.length();++i)
            inputString[i]=toupper(inputString[i]);
        
        curToken.tokenType=TokenType::FakeInstruct;
        if(inputString=="ORIG"||inputString=="END"||inputString=="FILL"||inputString=="BLKW"||inputString=="STRINGZ"){
            curToken.strValue=inputString;
        }
        else{
            throw "Invalid Fake Instruction";
        }
        return ;
    }
    //immi hex or label
    if(nextChar=='x'||nextChar=='X'){
        string inputString="";
        char charReserve=nextChar;
        int neg=1;
        bool is_number=0;
        nextChar=fGetChar(fp,line);//eat x
        if(nextChar=='-'){
            neg=-1;
            is_number=1;
            nextChar=fGetChar(fp,line);//eat -
        }
        else if(nextChar=='+'){
            neg=1;
            is_number=1;
            nextChar=fGetChar(fp,line);//eat +
        }
        while(isdigit(nextChar)||(nextChar>='A'&&nextChar<='F')||(nextChar>='a'&&nextChar<='f')){
            inputString+=nextChar;
            nextChar=fGetChar(fp,line);
        }
        if(isalpha(nextChar)||nextChar=='_'){
            if(is_number){
                throw "Syntax error";
            }
            //is label
            while(isalpha(nextChar)||isdigit(nextChar)||nextChar=='_'){
                inputString+=nextChar;
                nextChar=fGetChar(fp,line);
            }
            curToken.tokenType=TokenType::Label;
            curToken.strValue=charReserve+inputString;
            return;
        }//x X can be label
        if(inputString==""){
            curToken.strValue=charReserve;
            curToken.tokenType=TokenType::Label;
            return;
        }
        
        curToken.numValue=(convertImm(inputString,16)*neg);
        curToken.tokenType=TokenType::ImmediateNumber;
        return;
    }
    //immi deci
    if(nextChar=='#'||isdigit(nextChar)||nextChar=='-'||nextChar=='+'){
        string inputString="";
        if(nextChar=='#')
            nextChar=fGetChar(fp,line);//eat #
        int neg=1;
        if(nextChar=='-'){
            neg=-1;
            nextChar=fGetChar(fp,line);//eat -
        }
        else if(nextChar=='+'){
            nextChar=fGetChar(fp,line);//eat +
        }
        while(isdigit(nextChar)){
            inputString+=nextChar;
            nextChar=fGetChar(fp,line);
        }
        if(inputString==""){
            throw "Illegal decimal Immediate Number";
        }
        curToken.numValue=(convertImm(inputString,10)*neg);
        curToken.tokenType=TokenType::ImmediateNumber;
        return;
    }
    //string
    if(nextChar=='"'){
        string inputString="";
        nextChar=fGetChar(fp,line);//eat "
        while((nextChar!='"'&&nextChar!=EOF)){
            inputString+=nextChar;
            if(nextChar=='\\'){
                nextChar=fGetChar(fp,line);//eat char after "\"
                if(nextChar==EOF){
                    throw "expect '\"' for end of string";
                }
                inputString+=nextChar;
            }
            nextChar=fGetChar(fp,line);
        }
        curToken.tokenType=TokenType::String;
        if(nextChar==EOF){
            throw "expect '\"' for end of string";
        }
        nextChar=fGetChar(fp,line);//eat "
        //char *convertStr=new char[inputString.length()];
        char *convertStr=(char*)malloc((inputString.length()+1)*sizeof(char));
        sprintf(convertStr, "%s",inputString.c_str());
        curToken.strValue=convertStr;
        //delete[] convertStr;
        free(convertStr);
        return;
    }
    //[a-zA-Z_][a-zA-Z0-9_]*
    if(isalpha(nextChar)||nextChar=='_'){
        string inputString="",upInputString="";
        while(isalpha(nextChar)||isdigit(nextChar)||nextChar=='_'){
            inputString+=nextChar;
            nextChar=fGetChar(fp,line);
        }
        // to upper case
        for(int i=0;i<inputString.length();++i)
            upInputString+=toupper(inputString[i]);
        //instruction
        if(instructionMap.find(upInputString)!=instructionMap.end()){
            curToken.strValue=upInputString;
            curToken.numValue=instructionMap[upInputString];
            curToken.tokenType=TokenType::Instruction;
        }
        //register
        else if(inputString.length()==2&&(toupper(inputString[0])=='R')&&inputString[1]>='0'&&inputString[1]<='7'){
            curToken.numValue=(int)(inputString[1]-'0');
            curToken.tokenType=TokenType::Register;
        }
        //label
        else {
            curToken.strValue=inputString;
            curToken.tokenType=TokenType::Label;
        }
        return;
    }
    //End Of File
    if(nextChar==EOF){
        curToken.tokenType=TokenType::EndOfFile;
        return;
    }
    //comma
    if(nextChar==','){
        curToken.tokenType=TokenType::Comma;
        nextChar=fGetChar(fp,line);              //eat ,
        return;
    }
    throw "unexpected character \""+nextChar+'"';
    curToken.tokenType=TokenType::Other;
    curToken.strValue=nextChar;
    nextChar=fGetChar(fp,line);                  //eat other char
    return;
}

void initialize(){
    instructionMap["ADD"]=      0b0001;
    instructionMap["AND"]=      0b0101;
    instructionMap["BR"]=       0b0000;
    instructionMap["BRN"]=      0b0000;
    instructionMap["BRZ"]=      0b0000;
    instructionMap["BRP"]=      0b0000;
    instructionMap["BRNZ"]=     0b0000;
    instructionMap["BRZP"]=     0b0000;
    instructionMap["BRNP"]=     0b0000;
    instructionMap["BRNZP"]=    0b0000;
    instructionMap["JMP"]=      0b1100;
    instructionMap["JSR"]=      0b0100;
    instructionMap["JSRR"]=     0b0100;
    instructionMap["LD"]=       0b0010;
    instructionMap["LDI"]=      0b1010;
    instructionMap["LDR"]=      0b0110;
    instructionMap["LEA"]=      0b1110;
    instructionMap["NOT"]=      0b1001;
    instructionMap["RET"]=      0b1100;
    instructionMap["RTI"]=      0b1000;
    instructionMap["ST"]=       0b0011;
    instructionMap["STI"]=      0b1011;
    instructionMap["STR"]=      0b0111;
    instructionMap["TRAP"]=     0b1111;
    instructionMap["GETC"]=     0b1111; //x20
    instructionMap["OUT"]=      0b1111; //x21
    instructionMap["PUTS"]=     0b1111; //x22
    instructionMap["IN"]=       0b1111; //x23
    instructionMap["PUTSP"]=    0b1111; //x24
    instructionMap["HALT"]=     0b1111; //x25
    instructionMap["NOP"]=      0b0000; 
}

string IntToStr(int value,int width){
    string tempStr;
    for(int i=width-1;i>=0;--i){
        if(value&(1<<i)){
            tempStr+="1";
        }
        else tempStr+="0";
    }
    return tempStr;
}

string immiConvert(int value,int width){
    if(value>=-(1<<(width-1))&&value<=((1<<(width-1))-1))
        return IntToStr(value,width);
    else{
        //throw to_string(value)+" can't be represent in imm"+to_string(width);
        throw "immi out of range";
    }

}

void eatComma(token &curToken,FILE *fpi,int &line){
    if(curToken.tokenType ==(int)TokenType::Comma){
        getToken(curToken, fpi, line);//eat ,
    }
    return;
}

string escapeConvert(string strIn){
    string strOut="";
    for(int i=0; i<strIn.length();++i){
        if(strIn[i]=='\\'){
            i++;
            switch(strIn[i]){
                case '"':
                case '\\':
                case '\'':strOut+=strIn[i];
                    break;
                case '?':strOut+='\?';
                    break;
                case 'a':strOut+='\a';
                    break;
                case 'b':strOut+='\b';
                    break;
                case 'f':strOut+='\f';
                    break;
                case 'n':strOut+='\n';
                    break;
                case 'r':strOut+='\r';
                    break;
                case 't':strOut+='\t';
                    break;
                case 'v':strOut+='\v';
                    break;
                case '0':strOut+='\0';
                    break;
                default:strOut+=strIn[i];
                    break;
            }
        }
        else{
            strOut+=strIn[i];
        }
    }
    return strOut;
}

void asm_file(string fileNameIn,int &line,map<string,int> &labelMap,vector <unit> &instructionVector){
    int index=0;
    string baseName = fileNameIn.substr(0,fileNameIn.length()-4);
    
    FILE *fpi= fopen(fileNameIn.c_str(), "r");
    if(fpi==NULL){
        std::cout<<"Could not open file "<<fileNameIn<<endl;
        std::exit(0);
    }

    ofstream fileOut;
    fileOut.open (baseName+"_own.obj",ios::out | ios::ate | ios::binary|ios::trunc);
    if(!fileOut.is_open()){
        std::cout<<"Can't open "<<baseName+"_own.obj !"<<endl;
        std::exit(0);
    }
    ofstream fileOutSym;
    fileOutSym.open (baseName+"_own.sym",ios::out | ios::ate |ios::trunc);
    if(!fileOutSym.is_open()){
        std::cout<<"Can't open "<<baseName+"_own.sym !"<<endl;
        std::exit(0);
    }
    int origPos;
    
    instructionVector.clear();
    token curToken;
    getToken(curToken, fpi, line);

    line=1;
    index=0;
    std::cout<<"Assemblying file "<<fileNameIn<<endl;
    std::cout<<"Pass1:";
    //orig
    if(curToken.tokenType ==(int)TokenType::FakeInstruct&&curToken.strValue=="ORIG"){
        getToken(curToken, fpi, line);
        if(curToken.tokenType==(int)TokenType::ImmediateNumber){
            if(((unsigned int)curToken.numValue>=0)&&((unsigned int)curToken.numValue<=0xffff)){
                origPos=curToken.numValue;
                unit newUnit=unit();
                newUnit.instructCode=immiConvert(origPos,16);
                instructionVector.push_back(newUnit);
            }
            else{
                throw "Origin address out of range!";
            }
        }
        else{
            throw "Expected origin address";
        }
    }
    else{
        throw "Expected .ORIG";
    }

    while(1){
        getToken(curToken, fpi, line);
        if(curToken.tokenType ==(int)TokenType::EndOfFile){
            throw "Unexpected end of file";
        }
        unit newUnit=unit();
        if(curToken.tokenType ==(int)TokenType::Label){
            if(labelMap.find(curToken.strValue)!=labelMap.end()){
                throw ("Duplicate label \""+curToken.strValue+"\"");
                //throw "Duplicate label ";
            }
            labelMap[curToken.strValue]=origPos+index;
            getToken(curToken, fpi, line);//eat label
        }
        if(curToken.tokenType ==(int)TokenType::FakeInstruct){
            if(curToken.strValue=="FILL"){
                getToken(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::Label){
                    newUnit.haveLabel=1;
                    newUnit.labelName=curToken.strValue;
                    newUnit.numOffset=16;
                }
                else if(curToken.tokenType ==(int)TokenType::ImmediateNumber){
                    if((curToken.numValue>=-0x8000)&&(curToken.numValue<=0xffff)){
                        newUnit.instructCode=IntToStr(curToken.numValue,16);
                    }
                    else{
                        throw "Fill value out of range";
                    }
                }
                else{
                    throw "Expect label or Immediate Number after \".FILL\"";
                }
                instructionVector.push_back(newUnit);
                index++;
            }
            else if(curToken.strValue=="BLKW"){
                getToken(curToken, fpi, line);
                newUnit.instructCode=string(16,'0');
                if(curToken.tokenType ==(int)TokenType::ImmediateNumber){
                    if(curToken.numValue<0)curToken.numValue+=0x10000;
                    for(int j=0;j<curToken.numValue;++j){
                        instructionVector.push_back(newUnit);
                    }
                    index+=curToken.numValue;
                }
                else{
                    throw "Expect immediate Number after \".BLKW\"";
                }
            }
            else if(curToken.strValue=="STRINGZ"){
                getToken(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::String){
                    curToken.strValue=escapeConvert(curToken.strValue);
                    for(int j=0; j<curToken.strValue.length(); j++){
                        newUnit.instructCode=IntToStr(curToken.strValue[j],16);
                        instructionVector.push_back(newUnit);
                    }
                    newUnit.instructCode=IntToStr(0,16);// \0
                    instructionVector.push_back(newUnit);
                    index+=curToken.strValue.length()+1;
                }
            }
            else if(curToken.strValue=="END"){
                /*
                getToken(curToken, fpi, line);
                if(curToken.tokenType!=(int)TokenType::EndOfFile){
                    throw "Redundant content after \".END\"";
                }*/
                break;
            }
            else {
                throw "Unexpected Fake Instruction \""+curToken.strValue+'"';
            }
        }
        else if(curToken.tokenType ==(int)TokenType::Instruction){
            newUnit.instructCode=IntToStr(curToken.numValue,4);
            switch (curToken.numValue)
            {
            case 0b0001:
            case 0b0101://ADD AND
                getToken(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::Register){
                    newUnit.instructCode+=IntToStr(curToken.numValue,3);
                }
                else{
                    throw "Expected register";
                }
                getToken(curToken, fpi, line);
                eatComma(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::Register){
                    newUnit.instructCode+=IntToStr(curToken.numValue,3);
                }
                else{
                    throw "Expected register";
                }
                getToken(curToken, fpi, line);
                eatComma(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::Register){
                    newUnit.instructCode+=IntToStr(curToken.numValue,6);
                }
                else if(curToken.tokenType ==(int)TokenType::ImmediateNumber){
                    newUnit.instructCode+="1"+immiConvert(curToken.numValue,5);
                }
                else{
                    throw "Expected register or immediate number ";
                }
                break;
            case 0b0000://BR or NOP
                if(curToken.strValue=="NOP"){
                    newUnit.instructCode=immiConvert(0,16);
                }
                else {
                    if(curToken.strValue=="BR"||curToken.strValue=="BRNZP")
                        newUnit.instructCode+="111";
                    if(curToken.strValue=="BRN")newUnit.instructCode+="100";
                    if(curToken.strValue=="BRZ")newUnit.instructCode+="010";
                    if(curToken.strValue=="BRP")newUnit.instructCode+="001";
                    if(curToken.strValue=="BRNZ")newUnit.instructCode+="110";
                    if(curToken.strValue=="BRNP")newUnit.instructCode+="101";
                    if(curToken.strValue=="BRZP")newUnit.instructCode+="011";
                    getToken(curToken, fpi, line);
                    if(curToken.tokenType==(int)TokenType::ImmediateNumber){
                        newUnit.instructCode+=immiConvert(curToken.numValue,9);
                    }
                    else if(curToken.tokenType==(int)TokenType::Label){
                        newUnit.labelName=curToken.strValue;
                        newUnit.haveLabel=1;
                        newUnit.numOffset=9;
                    }
                    else {
                        throw "Expected Immediate Number or label";
                    }
                }
                break;
            case 0b1100://JMP RET
                if(curToken.strValue=="RET") {
                    newUnit.instructCode+="000111000000";
                }
                else{
                    getToken(curToken, fpi, line);
                    if(curToken.tokenType ==(int)TokenType::Register){
                        newUnit.instructCode+="000"+IntToStr(curToken.numValue,3)+"000000";
                    }
                    else{
                        throw "Expected register";
                    }
                }
                break;
            case 0b0100://JSR JSRR
                if(curToken.strValue=="JSR"){
                    getToken(curToken, fpi, line);
                    if(curToken.tokenType ==(int)TokenType::ImmediateNumber){
                        newUnit.instructCode+="1"+immiConvert(curToken.numValue,11);
                    }
                    else if(curToken.tokenType==(int)TokenType::Label){
                        newUnit.instructCode+="1";
                        newUnit.labelName=curToken.strValue;
                        newUnit.haveLabel=1;
                        newUnit.numOffset=11;
                    }
                    else{
                        throw "Expected immediate number or label";
                    }
                }
                else{
                    getToken(curToken, fpi, line);
                    if(curToken.tokenType ==(int)TokenType::Register){
                        newUnit.instructCode+="000"+IntToStr(curToken.numValue,3)+"000000";
                    }
                    else{
                        throw "Expected register";
                    }
                }
                break;
            case 0b0010:
            case 0b0011://LD ST
            case 0b1010:
            case 0b1011://LDI STI
            case 0b1110://LEA
                getToken(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::Register){
                    newUnit.instructCode+=IntToStr(curToken.numValue,3);
                }
                else{
                    throw "Expected register";
                }
                getToken(curToken, fpi, line);
                eatComma(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::ImmediateNumber){
                    newUnit.instructCode+=immiConvert(curToken.numValue,9);
                }
                else if(curToken.tokenType==(int)TokenType::Label){
                    newUnit.labelName=curToken.strValue;
                    newUnit.haveLabel=1;
                    newUnit.numOffset=9;
                }
                else{
                    throw "Expected immediate number or label";
                }
                break;
            case 0b0110:
            case 0b0111://LDR STR
                getToken(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::Register){
                    newUnit.instructCode+=IntToStr(curToken.numValue,3);
                }
                else{
                    throw "Expected register";
                }
                
                getToken(curToken, fpi, line);
                eatComma(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::Register){
                    newUnit.instructCode+=IntToStr(curToken.numValue,3);
                }
                else{
                    throw "Expected register";
                }
                getToken(curToken, fpi, line);
                eatComma(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::ImmediateNumber){
                    newUnit.instructCode+=immiConvert(curToken.numValue,6);
                }
                else{
                    throw "Expected immediate number";
                }
                
                break;
            case 0b1001://NOT
                getToken(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::Register){
                    newUnit.instructCode+=IntToStr(curToken.numValue,3);
                }
                else{
                    throw "Expected register";
                }
                getToken(curToken, fpi, line);
                eatComma(curToken, fpi, line);
                if(curToken.tokenType ==(int)TokenType::Register){
                    newUnit.instructCode+=IntToStr(curToken.numValue,3)+"111111";
                }
                else{
                    throw "Expected register";
                }

                break;
            case 0b1000://RTI
                newUnit.instructCode+=string(12,'0');
                break;
            case 0b1111://TRAP
                if(curToken.strValue=="GETC")
                    newUnit.instructCode+="0000"+IntToStr(0x20,8);
                else if(curToken.strValue=="OUT")
                    newUnit.instructCode+="0000"+IntToStr(0x21,8);
                else if(curToken.strValue=="PUTS")
                    newUnit.instructCode+="0000"+IntToStr(0x22,8);
                else if(curToken.strValue=="IN")
                    newUnit.instructCode+="0000"+IntToStr(0x23,8);
                else if(curToken.strValue=="PUTSP")
                    newUnit.instructCode+="0000"+IntToStr(0x24,8);
                else if(curToken.strValue=="HALT")
                    newUnit.instructCode+="0000"+IntToStr(0x25,8);
                else{
                    getToken(curToken, fpi, line);
                    if(curToken.tokenType ==(int)TokenType::ImmediateNumber){
                        if((unsigned int)curToken.numValue>=0&&(unsigned int)curToken.numValue<=0xFF){
                            newUnit.instructCode+="0000"+IntToStr(curToken.numValue,8);
                        }
                        else{
                            throw "Trap vector number out of range";
                        }
                    }
                    else{
                        throw "Expect trap vector number";
                    }
                }
                break;                    
            default:
                throw "No such instruction"+curToken.strValue;
                break;
            }
            instructionVector.push_back(newUnit);
            index++;
        }
        else{
            throw "Unexpected character";
        }
        
        if(index>0xFFFF){
            throw "Address access exceeded 0xFFFF";
        }
    }

    std::cout<<"No Error"<<endl;
    std::cout<<"Pass2:";


    // std::cout<<index<<endl;

    for(int j=1;j<index+1;j++){
        if(instructionVector.at(j).haveLabel){
            if(labelMap.find(instructionVector.at(j).labelName)!=labelMap.end()){
                int numOffset=instructionVector.at(j).numOffset;
                if(numOffset==16)
                    instructionVector.at(j).instructCode=immiConvert(labelMap[instructionVector.at(j).labelName],16);
                else
                    instructionVector.at(j).instructCode+=immiConvert(labelMap[instructionVector.at(j).labelName]-(origPos+j),numOffset);
            }
            else{
                throw "Label \""+instructionVector.at(j).labelName+"\" is not found";
            }
        }
    }
    std::cout<<"No Error"<<endl;

    //generate .obj


    for(int j=0;j<index+1;j++){
        if(instructionVector.at(j).instructCode.length()!=16)
            throw "Instruction length is "+to_string(instructionVector.at(j).instructCode.length());
        unsigned short doublebyte=0;
        for(int k=0;k<16;++k){
            bool temp=instructionVector.at(j).instructCode[k]=='1';
            doublebyte=doublebyte*2+temp;
        }
        char output1=doublebyte>>8,output2=doublebyte;
        fileOut.write(&output1,1);
        fileOut.write(&output2,1);
    }
    fileOutSym<<"//Symbol Name		Page Address\n//----------------	------------\n";

    for(map<string,int>::iterator it=labelMap.begin();it!=labelMap.end();it++){
        fileOutSym<<"//\t"<<it->first<<"\t\t\t\t\t"<<setfill('0') << setw(4)<<std::uppercase<<std::hex<<it->second<<"\n";
    }

    fclose(fpi);
    fileOut.close();
    fileOutSym.close();
    std::cout<<"Succeed, output file is :"<<baseName+"_own.obj "<<baseName+"_own.sym"<<endl;
    instructionVector.clear();
    return;
}

void hex_file(string fileNameIn,int &line){
    string baseName = fileNameIn.substr(0,fileNameIn.length()-4);
    
    FILE *fpi= fopen(fileNameIn.c_str(), "r");
    if(fpi==NULL){
        std::cout<<"Could not open file "<<fileNameIn<<endl;
        std::exit(0);
    }

    ofstream fileOut;
    fileOut.open (baseName+"_own.obj",ios::out | ios::ate | ios::binary|ios::trunc);
    if(!fileOut.is_open()){
        std::cout<<"Can't open "<<baseName+"_own.obj !"<<endl;
        std::exit(0);
    }

    std::cout<<"Converting file "<<fileNameIn<<" from base 16"<<endl;
    char c=fGetChar(fpi,line);
    while(c!=EOF){
        if(c==';'){
            while(c!='\n'&&c!=EOF)c=fGetChar(fpi,line);
        }
        while(c=='\n'&&c!=EOF)c=fGetChar(fpi,line);
        while(c==' '&&c!=EOF)c=fGetChar(fpi,line);
        if(c==EOF)break;
        string data="";
        int i=0;
        while(1){
            if(isdigit(c)||(c>='A'&&c<='F')||(c>='a'&&c<='f')){
                data+=c;
                c=fGetChar(fpi,line);
                i++;
            }
            while(c==' '&&c!=EOF)c=fGetChar(fpi,line);
            if(c==EOF||c=='\n'||c==';')break;
        }
        if(i<4){
            throw "Less character than expected";
        }
        if(i>4){
            throw "More characters than expected";
        }
        int temp=stoi(data,nullptr,16);
        char byte1=temp>>8,byte2=temp%256;
        fileOut.write(&byte1,1);
        fileOut.write(&byte2,1);
    }
    
    fileOut.close();
    std::cout<<"Succeed, output file is "<<baseName+"_own.obj"<<endl;

}

void bin_file(string fileNameIn,int &line){
    string baseName = fileNameIn.substr(0,fileNameIn.length()-4);
    
    FILE *fpi= fopen(fileNameIn.c_str(), "r");
    if(fpi==NULL){
        std::cout<<"Could not open file "<<fileNameIn<<endl;
        std::exit(0);
    }

    ofstream fileOut;
    fileOut.open (baseName+"_own.obj",ios::out | ios::ate | ios::binary|ios::trunc);
    if(!fileOut.is_open()){
        std::cout<<"Can't open "<<baseName+"_own.obj !"<<endl;
        std::exit(0);
    }

    std::cout<<"Converting file "<<fileNameIn<<" from base 2"<<endl;
    char c=fGetChar(fpi,line);
    while(c!=EOF){
        if(c==';'){
            while(c!='\n'&&c!=EOF)c=fGetChar(fpi,line);
        }
        while(c=='\n'&&c!=EOF)c=fGetChar(fpi,line);
        while(c==' '&&c!=EOF)c=fGetChar(fpi,line);
        if(c==EOF)break;
        string data="";
        int i=0;
        while(1){
            if(c=='0'||c=='1'){
                data+=c;
                c=fGetChar(fpi,line);
                i++;
            }
            while(c==' '&&c!=EOF)c=fGetChar(fpi,line);
            if(c==EOF||c=='\n'||c==';')break;
        }
        if(i<16){
            throw "Less bits than expected";
        }
        if(i>16){
            throw "More bits than expected";
        }
        int temp=stoi(data,nullptr,2);
        char byte1=temp>>8,byte2=temp%256;
        fileOut.write(&byte1,1);
        fileOut.write(&byte2,1);
    }
    
    fileOut.close();
    std::cout<<"Succeed, output file is "<<baseName+"_own.obj"<<endl;
}

int main(int argc, char* argv[]){
    if(argc<2){
        printf("Usage: LC3assemblier.exe [filename]\n");//TODO
        std::exit(0);
    }
    initialize();
    
    map <string,int> labelMap;
    
    vector <unit> instructionVector;
    int line,index;
    try{
        for(int i=1;i<argc;++i){
            string fileNameIn=argv[i];
            string baseName;
            if(fileNameIn.length()>4){
                if ((fileNameIn.substr(fileNameIn.length()-4,4)==".asm"))
                    asm_file(fileNameIn,line,labelMap,instructionVector);
                else if ((fileNameIn.substr(fileNameIn.length()-4,4)==".hex"))
                    hex_file(fileNameIn,line);
                else if((fileNameIn.substr(fileNameIn.length()-4,4)==".bin"))
                    bin_file(fileNameIn,line);
                else{
                    printf("Wrong input file %s",argv[i]);
                    std::exit(0);
                }
            }
            else{
                printf("Wrong input file %s",argv[i]);
                std::exit(0);
            }
            
        }
    }catch (const string msg) {
     std::cout << "Error at line " << line << ": " << msg << endl;
    }catch (const std::exception& ex) {
      std::cout << ex.what() << std::endl;
    } 

    return 0;
}
