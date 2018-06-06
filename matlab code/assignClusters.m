function [clusteredData, medoidIdx] = assignClusters(clusteredData, medoidIdx, minClusterDissim, k, valuesPerSet)
%UNTITLED8 Summary of this function goes here
%   Detailed explanation goes here

clusterDissim = zeros(k,1);
%     begin
for v = 1:valuesPerSet
    [clusteredData(v,3),clusteredData(v,4)] = closestClusterMedoid(clusteredData(v,:), clusteredData(medoidIdx,:));
    clusterDissim(clusteredData(v,3),1) = clusterDissim(clusteredData(v,3),1) + clusteredData(v,4);
end
if minClusterDissim == (zeros(k,1) - 1)
    minClusterDissim = clusterDissim;
elseif minClusterDissim > clusterDissim
    minClusterDissim = clusterDissim;
end
%     end of first run
% consider all the points in a cluster as medoids one after another and
% find the one for which cluster dissimilarity is minimized.

[newMedoidIdx, minClusterDissim] = findBestMedoidsForGivenClusters(clusteredData, medoidIdx, minClusterDissim);
if newMedoidIdx ~= medoidIdx
    medoidIdx = newMedoidIdx;
    [clusteredData, medoidIdx] = assignClusters(clusteredData, medoidIdx, minClusterDissim, k, valuesPerSet);
end
end

