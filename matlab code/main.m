close all; clear all; clc;

%% Read data from file

readPath = 'D:\Kaustuv\datasets\clusteredData\';
writePath = 'D:\Kaustuv\datasets\trainedData\';
% fileName = 'frame1_subject1_set1.txt';
% numOfValuesInDataset = 1000; %dataset specific

% f = frame number; s = subject number
numFrames = 3;
numSubjects = 4;
valuesPerSet = 1800;
dataDimension = 2;
data = zeros (numFrames,numSubjects,valuesPerSet,dataDimension) -1;
for f = 1:numFrames
    for s = 1:numSubjects
%         data(f,s,:,:) = readFile(readPath,['frame', num2str(f), '_subject', num2str(s), '_set1.txt'],valuesPerSet, dataDimension);
     data(f,s,:,:) = readFile(readPath,['frame', num2str(f), '_subject',num2str(s), '_data.txt'],valuesPerSet, dataDimension);
   
    end
end

%% Finding Mean and Variance of Dataset assuming Gaussian Distribution

meanData = zeros(numFrames,numSubjects,1,2);
sigmaData = zeros(numFrames,numSubjects,1,2);
for f = 1:numFrames
    for s = 1:numSubjects
        frameSubjCutout = permute(data(f,s,:,:),[3 4 1 2]);
%         frameSubjCutout = frameSubjCutout(:,:,1,1);
%         size(frameSubjCutout)
        dataCutout = [];
        for iterLoop = 1:valuesPerSet
            if frameSubjCutout(iterLoop,:) == [-1 -1]
                break;
            else
                dataCutout = [dataCutout; frameSubjCutout(iterLoop,:)];
            end
        end
%         dataCutout
%         size(dataCutout)
        meanData(f,s,:,:) = mean(dataCutout(:,:));
        sigmaData(f,s,:,:) = sqrt(var(dataCutout(:,:)));
    end
end

%% Finding fuzzy membership functions corresponding to each frame and each subject

dim1 = 0:0.5:180;
dim2 = 0:0.5:180;

membershipF = zeros(numFrames,numSubjects,length(dim1),length(dim2));

for f = 1:numFrames
    for s = 1:numSubjects
        dummyMembership = [];
        constantFactor = (sigmaData(f,s,1,1)*sigmaData(f,s,1,2)*2*pi);
        for dimIter = 0:0.5:180
            dummyMembership = [dummyMembership ;exp(-0.5*(((dim1 - meanData(f,s,1,1))/sigmaData(f,s,1,1)).^2+((dimIter - meanData(f,s,1,2))/sigmaData(f,s,1,2)).^2))/constantFactor];
        end
        membershipF(f,s,:,:) = dummyMembership;
    end
end
clear dummyMembership;
%% Finding max and min fuzzy memberships for each interval type-2 fuzzy set

maxFuzzySet=1;
minFuzzySet=2;
membershipMaxMinF = zeros(numFrames,2,length(dim1),length(dim2));
membershipMaxMinF(:,1,:,:) = max(membershipF,[],2);
membershipMaxMinF(:,2,:,:) = min(membershipF,[],2);
membershipMaxMinF = membershipMaxMinF/max(membershipMaxMinF(:));

%% 3D Surface plot of the gaussian menbership function corresponding to the dataset
plotMembership = permute(membershipMaxMinF,[3 4 1 2]);

for f = 1:numFrames
    figure, surf(dim1,dim2,plotMembership(:,:,f,1));
    title(['MaxPlot for frame: ', num2str(f)]);
    xlabel('Shoulder Angle ->');
    ylabel('Elbow Angle ->');
    figure, surf(dim1,dim2,plotMembership(:,:,f,2));
    title(['MinPlot for frame: ', num2str(f)]);
    xlabel('Shoulder Angle ->');
    ylabel('Elbow Angle ->');
end

%% Conversion from type-2 to type-1 for each frame, considering mean, forming a look up table for membership

defuzzMembership = mean(membershipMaxMinF,2);
defuzzMembership = permute(defuzzMembership,[4 3 1 2]);
defuzzMembership = defuzzMembership(:,:,:,1);

frame1mem = defuzzMembership(:,:,1);
fid = fopen([writePath, 'frame1membership.txt'],'wt');
for iter = 1:size(frame1mem,1)
    fprintf(fid,'%1.8f',frame1mem(iter,:));
%     fprintf(fid,'\n');
end
fclose(fid);

frame2mem = defuzzMembership(:,:,2);
fid = fopen([writePath, 'frame2membership.txt'],'wt');
for iter = 1:size(frame2mem,1)
    fprintf(fid,'%1.8f',frame2mem(iter,:));
%     fprintf(fid,'\n');
end
fclose(fid);

frame3mem = defuzzMembership(:,:,3);
fid = fopen([writePath, 'frame3membership.txt'],'wt');
for iter = 1:size(frame3mem,1)
    fprintf(fid,'%1.8f',frame3mem(iter,:));
%     fprintf(fid,'\n');
end
fclose(fid);

% fid = fopen([writePath, 'clusteredData\frame3Data.txt'],'wt');
% for iter = 1:size(frame3Data,1)
% fprintf(fid,'%1.8f, %1.8f;',frame3Data(iter,1),frame3Data(iter,2));
% %     fprintf(fid,'\n');
% end
% fclose(fid)

