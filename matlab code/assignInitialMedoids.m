function [ initialMedoidIdx ] = assignInitialMedoids(inputData,k)
%UNTITLED Summary of this function goes here
%   Detailed explanation goes here

workingData = zeros(size(inputData,1),5);
% First two columns hold input data
% Third Column: Nearest Medoid
% Fourth Column: Distance from nearest Medoid
% Fifth Column: Cumulative Distance

workingData(:,1:2) = inputData;
medoidIdx = [1 zeros(1,k-1)];

for iter = 1:k-1
    for v = 1:size(workingData,1)
        [workingData(v,3),workingData(v,4)] = closestClusterMedoid(workingData(v,:), workingData(medoidIdx(1:iter),:));
        if (v == 1)
            workingData(v,5) = uint16(workingData(v,4)^2);
        else
            workingData(v,5) = uint16(workingData(v-1,5) + (workingData(v,4)^2));
        end
    end
    randomNum = randi([1 workingData(end,5)],1,1);
    for v = 2:size(workingData,1)
        if(randomNum<=workingData(v,5))
            medoidIdx(iter+1) = v;
            break;
        end
    end
end

initialMedoidIdx = medoidIdx;
end

