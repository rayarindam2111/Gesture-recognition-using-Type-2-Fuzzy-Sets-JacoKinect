function [medoidIdx, minClusterDissim] = findBestMedoidsForGivenClusters(clusteredData, medoidIdx, minClusterDissim)
%UNTITLED7 Summary of this function goes here
%   Detailed explanation goes here


for clusterNum = 1:size(medoidIdx,2)
    for v = 1:size(clusteredData,1)
        if clusteredData(v,3) == clusterNum
            trialMedoid = clusteredData(v,:);
            clusterDissim = 0;
            for v2 = 1:size(clusteredData,1)
                if clusteredData(v2,3) == clusterNum
                    trialDataPt = clusteredData(v2,:);
                    clusterDissim = clusterDissim + distBW2obs( trialMedoid(1,1:2), trialDataPt(1,1:2) );
                end
            end
            if clusterDissim < minClusterDissim(clusterNum)
                minClusterDissim(clusterNum) = clusterDissim;
                medoidIdx(clusterNum) = v;
            end
        end
    end
end
end

