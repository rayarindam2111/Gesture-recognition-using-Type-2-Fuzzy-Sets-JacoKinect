close all; clear all; clc;

%% Read data from file

path = 'D:\Kaustuv\datasets\';
% fileName = 'subject1_set2.txt';

% s = subject number
numSubjects = 4;
valuesPerSet = 1800;
dataDimension = 2;
data = zeros(numSubjects,valuesPerSet,dataDimension);

for s = 1:numSubjects
    data(s,:,:) = readFile(path,['subject', num2str(s), '_set2.txt'],valuesPerSet);
end

% Shuffle data to randomise order in which data are considered
shuffledData = data(:,randperm(size(data,2)),:);

%% K-Medoids

k = 3; % Use Automated k detection later

%  perform clustering for each subject -> obtain T1Fuzzy Set for each
%  cluster, for each subject -> combine fuzzy set of all subjects
%  corresponding to similar clusters, to form k-type2fuzzysets

% c = cluster of which the particular data is a part of.
dummyData = permute(shuffledData,[2 3 1]);
prevMediod = zeros(k,dataDimension+2);
firsttime = 1;
for s = 1:numSubjects
    clusteredData = zeros(valuesPerSet,dataDimension+2);
    clusteredData(:,1:2) = dummyData(:,:,s); %Cutting out the part of data to be clustered, into a 2d data.
    %     Third column will store c.
    %     Fourth column will store distance from medoid
    
    %     Randomly pick k medoids.
    initialMedoidIdx = assignInitialMedoids(clusteredData(:,1:2),k);
    % initialMedoidIdx = [300+s 600+s 900+s];
    clusteredData(initialMedoidIdx,3) = (1:k);
    %
    medoidIdx = initialMedoidIdx;
    minClusterDissim = zeros(k,1) - 1;
    [clusteredData, medoidIdx] = assignClusters(clusteredData, medoidIdx, minClusterDissim, k, valuesPerSet);
    figure, stem3(clusteredData(:,1),clusteredData(:,2),clusteredData(:,3),'filled')
    hold on;
    X1 = clusteredData(:,1);
    Y1 = clusteredData(:,2);
    Z1 = clusteredData(:,3);
    stem3(X1(Z1>1),Y1(Z1>1), Z1(Z1>1),'filled','Color',[1 0 0]);
    hold on;
    X1 = clusteredData(:,1);
    Y1 = clusteredData(:,2);
    Z1 = clusteredData(:,3);
    stem3(X1(Z1>2),Y1(Z1>2), Z1(Z1>2),'filled','Color',[0 1 0]);
%     hold on;
%     X1 = clusteredData(:,1);
%     Y1 = clusteredData(:,2);
%     Z1 = clusteredData(:,3);
%     stem3(X1(Z1>3),Y1(Z1>3), Z1(Z1>3),'filled','Color',[1 0 1]);
%     hold on;
%     X1 = clusteredData(:,1);
%     Y1 = clusteredData(:,2);
%     Z1 = clusteredData(:,3);
%     stem3(X1(Z1>4),Y1(Z1>4), Z1(Z1>4),'filled','Color',[0 1 1]);
    hold off;
    title(['Clusters for subject: ', num2str(s)]);
    xlabel('Shoulder Angle');
    ylabel('Elbow Angle');
    zlabel('Cluster Number');
    %% Ordering clusters: farthest labelled 1, closest labelled k
    %
    if (firsttime==1)
        firsttime = 0;
        prevMediod = clusteredData(medoidIdx,:);
    end
    
    for i = 1:k
        valueToCheck = prevMediod(i, 1:2);
        minData = distBW2obs(valueToCheck,clusteredData(medoidIdx(1), 1:2));
        minIndex = 1;
        for j=2:k
            temp = distBW2obs(valueToCheck,clusteredData(medoidIdx(j), 1:2));
            if(temp<minData)
                minIndex = j;
                minData=temp;
            end
        end
        frameData = [];
        for v = 1:valuesPerSet
            if clusteredData(v,3) == minIndex
                frameData = [frameData; clusteredData(v,1:2)];
            end
        end
        fid = fopen([path, 'clusteredData\frame', num2str(i), '_subject',num2str(s),'_data.txt'],'wt');
        for iter = 1:size(frameData,1)
            fprintf(fid,'%f, %f;\n',frameData(iter,1),frameData(iter,2));
            %     fprintf(fid,'\n');
        end
        fclose(fid);
        % prevMediod(i,:) = clusteredData(medoidIdx(minindex),:);
        
    end
    %
end
