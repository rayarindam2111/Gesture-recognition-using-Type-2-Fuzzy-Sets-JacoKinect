function [ data ] = readFile( path, fileName, numOfValuesInDataset, dataDimension )
%UNTITLED2 Summary of this function goes here
%   Detailed explanation goes here

    if nargin < 4 % checks, if the number of input-parameters is less than 4
        dataDimension = 2;
    end
    % path = 'D:\Kaustuv\matlab code\';
    % fileName = 'F1_Shoulder180_Elbow180.txt';
    fileID = fopen([path fileName],'r');

    % numOfValuesInDataset = 1000; %dataset specific
    % dataDimension = 2;

     data = zeros(numOfValuesInDataset,dataDimension)-1;
    i=1;
    %data =[];
    while(~feof(fileID))
        data(i,:) = fscanf(fileID,'%f, %f;\n',[1 2]);
        i = i+1;
    end
    fclose(fileID);

%     data = data';
end
